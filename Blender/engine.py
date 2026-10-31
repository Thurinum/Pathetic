from __future__ import annotations

import json
import os
import shutil
import struct
import subprocess
import tempfile
import traceback

import bpy

from . import exporter
from .preferences import get_executable, get_preferences

CREATE_NO_WINDOW = 0x08000000 if os.name == "nt" else 0


def _srgb_to_linear(value: float) -> float:
    if value <= 0.04045:
        return value / 12.92
    return ((value + 0.055) / 1.055) ** 2.4


def _load_raw_pixels(path: str, expected_width: int, expected_height: int) -> list[float]:
    with open(path, "rb") as handle:
        header = handle.read(8)
        if len(header) != 8:
            raise RuntimeError("Pathetic output is missing its header.")

        width, height = struct.unpack("<II", header)
        if width != expected_width or height != expected_height:
            raise RuntimeError(
                f"Pathetic returned {width}x{height}, expected {expected_width}x{expected_height}."
            )

        data = handle.read()
        if len(data) != width * height * 4:
            raise RuntimeError("Pathetic output has an unexpected size.")

    try:
        import numpy as np
    except ImportError:
        return _load_raw_pixels_python(data, width, height)

    rgba = np.frombuffer(data, dtype=np.uint8).reshape((height, width, 4))
    rgba = rgba[::-1].astype(np.float32) / 255.0

    rgb = rgba[..., :3]
    linear = np.where(rgb <= 0.04045, rgb / 12.92, np.power((rgb + 0.055) / 1.055, 2.4))
    rgba[..., :3] = linear

    return rgba.reshape(-1).tolist()


def _load_raw_pixels_python(data: bytes, width: int, height: int) -> list[float]:
    pixels = [0.0] * (width * height * 4)

    for y in range(height):
        source_row = y * width * 4
        destination_row = (height - 1 - y) * width * 4

        for x in range(width):
            source = source_row + x * 4
            destination = destination_row + x * 4
            pixels[destination + 0] = _srgb_to_linear(data[source + 0] / 255.0)
            pixels[destination + 1] = _srgb_to_linear(data[source + 1] / 255.0)
            pixels[destination + 2] = _srgb_to_linear(data[source + 2] / 255.0)
            pixels[destination + 3] = data[source + 3] / 255.0

    return pixels


class PatheticRenderEngine(bpy.types.RenderEngine):
    bl_idname = "PATHETIC"
    bl_label = "Pathetic"
    bl_use_preview = False
    bl_use_postprocess = False
    bl_use_shading_nodes_custom = False

    def render(self, depsgraph):
        scene = depsgraph.scene
        settings = scene.pathetic
        preferences = get_preferences()
        executable = get_executable()

        if not executable or not os.path.isfile(executable):
            self.error_set(
                "Pathetic executable not found. Set it in Preferences > Add-ons > Pathetic Renderer."
            )
            return

        temporary_directory = tempfile.mkdtemp(prefix="pathetic_")
        scene_path = os.path.join(temporary_directory, "scene.json")
        output_path = os.path.join(temporary_directory, "present.rgba")

        try:
            self.update_progress(0.05)
            request = exporter.build_request(scene, depsgraph, settings)

            with open(scene_path, "w", encoding="utf-8") as handle:
                json.dump(request, handle, separators=(",", ":"))

            command = [
                executable,
                "--headless",
                "--scene",
                scene_path,
                "--output",
                output_path,
            ]

            print(f"[Pathetic] Rendering {request['width']}x{request['height']} with {len(request['triangles'])} triangles")
            self.update_progress(0.1)

            process = subprocess.run(
                command,
                cwd=os.path.dirname(executable) or None,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                text=True,
                timeout=int(settings.timeout_seconds),
                creationflags=CREATE_NO_WINDOW,
            )

            if process.stdout:
                print(process.stdout)

            if process.returncode != 0:
                raise RuntimeError(
                    f"Pathetic exited with code {process.returncode}. See the console for details."
                )

            pixels = _load_raw_pixels(output_path, request["width"], request["height"])
            self.update_progress(0.9)

            if settings.force_standard_view:
                # Pathetic's present texture is already tonemapped. Standard avoids a
                # second view transform when Blender displays or saves the result.
                try:
                    scene.view_settings.view_transform = "Standard"
                    scene.view_settings.look = "None"
                    scene.view_settings.exposure = 0.0
                    scene.view_settings.gamma = 1.0
                except (AttributeError, TypeError):
                    pass

            result = self.begin_result(0, 0, request["width"], request["height"])
            try:
                layer = result.layers[0]
                combined = layer.passes.get("Combined")
                if combined is None:
                    combined = layer.passes[0]
                combined.rect = pixels
            finally:
                self.end_result(result)

            self.update_progress(1.0)
        except Exception as exception:
            traceback.print_exc()
            self.error_set(str(exception))
        finally:
            if settings.cleanup_temp_files and not settings.keep_alive:
                shutil.rmtree(temporary_directory, ignore_errors=True)

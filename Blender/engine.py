from __future__ import annotations

import json
import os
import shutil
import struct
import subprocess
import tempfile
import threading
import traceback

import bpy

from . import exporter
from .preferences import get_executable

CREATE_NO_WINDOW = 0x08000000 if os.name == "nt" else 0


def _srgb_to_linear(value: float) -> float:
    if value <= 0.04045:
        return value / 12.92
    return ((value + 0.055) / 1.055) ** 2.4


def _load_raw_pixels(path: str, expected_width: int, expected_height: int) -> list[list[float]]:
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

    return rgba.reshape((-1, 4)).tolist()


def _load_raw_pixels_python(data: bytes, width: int, height: int) -> list[list[float]]:
    pixels: list[list[float]] = []

    for y in range(height):
        source_row = (height - 1 - y) * width * 4

        for x in range(width):
            source = source_row + x * 4
            pixels.append([
                _srgb_to_linear(data[source + 0] / 255.0),
                _srgb_to_linear(data[source + 1] / 255.0),
                _srgb_to_linear(data[source + 2] / 255.0),
                data[source + 3] / 255.0,
            ])

    return pixels


def _flatten_pixels(pixels: list[list[float]]) -> list[float]:
    return [channel for pixel in pixels for channel in pixel]


def _run_request(
    executable: str,
    request: dict,
    timeout_seconds: int,
    temporary_directory: str,
) -> list[list[float]]:
    scene_path = os.path.join(temporary_directory, "scene.json")
    output_path = os.path.join(temporary_directory, "present.rgba")

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

    process = subprocess.run(
        command,
        cwd=os.path.dirname(executable) or None,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        timeout=int(timeout_seconds),
        creationflags=CREATE_NO_WINDOW,
    )

    if process.stdout:
        print(process.stdout)

    if process.returncode != 0:
        raise RuntimeError(
            f"Pathetic exited with code {process.returncode}. See the console for details."
        )

    return _load_raw_pixels(output_path, request["width"], request["height"])


def _force_standard_view(scene) -> None:
    try:
        scene.view_settings.view_transform = "Standard"
        scene.view_settings.look = "None"
        scene.view_settings.exposure = 0.0
        scene.view_settings.gamma = 1.0
    except (AttributeError, TypeError):
        pass


class PatheticRenderEngine(bpy.types.RenderEngine):
    bl_idname = "PATHETIC"
    bl_label = "Pathetic"
    bl_use_preview = False
    bl_use_postprocess = False
    bl_use_shading_nodes_custom = False

    def render(self, depsgraph):
        scene = depsgraph.scene
        settings = scene.pathetic
        executable = get_executable()

        if not executable or not os.path.isfile(executable):
            self.error_set(
                "Pathetic executable not found. Set it in Preferences > Add-ons > Pathetic Renderer."
            )
            return

        temporary_directory = tempfile.mkdtemp(prefix="pathetic_")

        try:
            self.update_progress(0.05)
            request = exporter.build_request(scene, depsgraph, settings)

            print(f"[Pathetic] Rendering {request['width']}x{request['height']} with {len(request['triangles'])} triangles")
            self.update_progress(0.1)

            pixels = _run_request(
                executable,
                request,
                int(settings.timeout_seconds),
                temporary_directory,
            )
            self.update_progress(0.9)

            if settings.force_standard_view:
                # Pathetic's present texture is already tonemapped. Standard avoids a
                # second view transform when Blender displays or saves the result.
                _force_standard_view(scene)

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

    def _viewport_state(self):
        state = getattr(self, "_viewport_state_cache", None)
        if state is None:
            state = {
                "lock": threading.Lock(),
                "pending": None,
                "result": None,
                "running": False,
                "poll_registered": False,
            }
            self._viewport_state_cache = state
        return state

    def _schedule_viewport(self, executable, request, timeout_seconds):
        state = self._viewport_state()
        with state["lock"]:
            state["pending"] = (executable, request, timeout_seconds)
            if state["running"]:
                return
            state["running"] = True

        if not state["poll_registered"]:
            state["poll_registered"] = True
            bpy.app.timers.register(self._viewport_poll, first_interval=0.05)

        threading.Thread(target=self._viewport_worker, daemon=True).start()

    def _viewport_worker(self):
        state = self._viewport_state()

        while True:
            with state["lock"]:
                job = state["pending"]
                state["pending"] = None
                if job is None:
                    state["running"] = False
                    return

            executable, request, timeout_seconds = job
            temporary_directory = tempfile.mkdtemp(prefix="pathetic_view_")
            try:
                pixels = _run_request(executable, request, timeout_seconds, temporary_directory)
                with state["lock"]:
                    state["result"] = (pixels, request["width"], request["height"])
            except Exception:
                traceback.print_exc()
            finally:
                shutil.rmtree(temporary_directory, ignore_errors=True)

    def _viewport_poll(self):
        state = self._viewport_state()
        with state["lock"]:
            result = state["result"]
            state["result"] = None
            running = state["running"]
            pending = state["pending"] is not None

        if result is not None:
            pixels, width, height = result
            self._viewport_pixels = pixels
            self._viewport_dimensions = (width, height)
            self._viewport_texture = None
            try:
                self.tag_redraw()
            except Exception:
                pass

        if running or pending:
            return 0.05

        state["poll_registered"] = False
        return None

    def view_update(self, context, depsgraph):
        scene = depsgraph.scene
        settings = scene.pathetic
        if not settings.viewport_preview:
            self._viewport_pixels = None
            self._viewport_texture = None
            return

        region = getattr(context, "region", None)
        region_data = getattr(context, "region_data", None)
        if region is None or region_data is None:
            return

        if region_data.view_perspective not in {"PERSP", "CAMERA"}:
            return

        executable = get_executable()
        if not executable or not os.path.isfile(executable):
            return

        scale = max(0.1, settings.viewport_scale / 100.0)
        width = max(1, int(region.width * scale))
        height = max(1, int(region.height * scale))

        try:
            perspective = region_data.perspective_matrix
            tan_half_fov = 1.0 / abs(perspective[1][1]) if abs(perspective[1][1]) > 1e-6 else 0.5
        except (AttributeError, IndexError, ZeroDivisionError):
            tan_half_fov = 0.5

        request = exporter.build_request(
            scene,
            depsgraph,
            settings,
            width=width,
            height=height,
            camera_matrix=region_data.view_matrix.inverted(),
            tan_half_fov=tan_half_fov,
            samples=settings.viewport_samples,
            samples_per_frame=settings.viewport_samples,
        )

        if settings.force_standard_view:
            _force_standard_view(scene)

        view_key = (
            width,
            height,
            scene.frame_current,
            tuple(round(value, 4) for row in region_data.view_matrix for value in row),
            tuple(round(value, 4) for row in region_data.perspective_matrix for value in row),
            int(settings.viewport_samples),
            int(settings.max_bounces),
            str(settings.tonemapper),
            float(settings.exposure),
            tuple(update.id.name for update in depsgraph.updates),
        )
        if view_key == getattr(self, "_viewport_key", None):
            return
        self._viewport_key = view_key

        self._schedule_viewport(
            executable,
            request,
            int(settings.timeout_seconds),
        )

    def view_draw(self, context, depsgraph):
        viewport_pixels = getattr(self, "_viewport_pixels", None)
        viewport_dimensions = getattr(self, "_viewport_dimensions", None)
        if viewport_pixels is None or viewport_dimensions is None:
            return

        try:
            import gpu
            from gpu_extras.presets import draw_texture_2d
        except ImportError:
            return

        width, height = viewport_dimensions
        texture = getattr(self, "_viewport_texture", None)
        if texture is None:
            flat = _flatten_pixels(viewport_pixels)
            buffer = gpu.types.Buffer("FLOAT", width * height * 4, flat)
            texture = gpu.types.GPUTexture(
                (width, height),
                format="RGBA16F",
                data=buffer,
            )
            self._viewport_texture = texture

        gpu.state.blend_set("ALPHA_PREMULT")
        self.bind_display_space_shader(depsgraph.scene)
        draw_texture_2d(
            texture,
            (0, 0),
            context.region.width,
            context.region.height,
        )
        self.unbind_display_space_shader()
        gpu.state.blend_set("NONE")

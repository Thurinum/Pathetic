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
from .bridge import PatheticBridge, log
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

    def _get_bridge(self, executable: str) -> PatheticBridge:
        bridge = getattr(self, "_viewport_bridge", None)
        if bridge is None or bridge.executable != executable:
            if bridge is not None:
                bridge.stop()
            bridge = PatheticBridge(executable)
            self._viewport_bridge = bridge
            self._viewport_loaded = False

        if not bridge.running:
            bridge.start()
            self._viewport_loaded = False

        return bridge

    def _ensure_viewport_poll(self) -> None:
        if getattr(self, "_viewport_poll_registered", False):
            return

        self._viewport_poll_registered = True
        bpy.app.timers.register(self._viewport_poll, first_interval=0.01)

    def _viewport_poll(self):
        bridge = getattr(self, "_viewport_bridge", None)
        if bridge is None:
            self._viewport_poll_registered = False
            return None

        latest = bridge.latest()
        if latest is not None:
            width, height, frame_index, pixels = latest
            if frame_index != getattr(self, "_viewport_frame_index", -1):
                self._viewport_frame_index = frame_index
                self._viewport_pixels = pixels
                self._viewport_dimensions = (width, height)
                self._viewport_texture = None
                try:
                    self.tag_redraw()
                except Exception:
                    pass
                area = getattr(self, "_viewport_area", None)
                if area is not None:
                    try:
                        area.tag_redraw()
                    except Exception:
                        pass

        if bridge.running:
            return 0.01

        self._viewport_poll_registered = False
        return None

    def _stop_viewport(self) -> None:
        bridge = getattr(self, "_viewport_bridge", None)
        if bridge is not None:
            bridge.stop()
        self._viewport_bridge = None
        self._viewport_loaded = False
        self._viewport_pixels = None
        self._viewport_texture = None

    def view_update(self, context, depsgraph):
        scene = depsgraph.scene
        settings = scene.pathetic
        if not settings.viewport_preview:
            self._stop_viewport()
            return

        region = getattr(context, "region", None)
        region_data = getattr(context, "region_data", None)
        if region is None or region_data is None:
            return

        self._viewport_area = getattr(context, "area", None)

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

        view_key = (
            width,
            height,
            scene.frame_current,
            tuple(round(value, 4) for row in region_data.view_matrix for value in row),
            tuple(round(value, 4) for row in region_data.perspective_matrix for value in row),
            tuple(update.id.name for update in depsgraph.updates),
        )
        settings_key = (
            int(settings.viewport_samples),
            int(settings.max_bounces),
            str(settings.tonemapper),
            float(settings.exposure),
        )

        if view_key == getattr(self, "_viewport_key", None) and settings_key == getattr(
            self, "_viewport_settings_key", None
        ):
            return

        bridge = self._get_bridge(executable)
        token = int(settings.viewport_reload_token)
        needs_load = (
            not getattr(self, "_viewport_loaded", False)
            or getattr(self, "_viewport_size", None) != (width, height)
            or getattr(self, "_viewport_reload_token", None) != token
            or (settings.viewport_auto_reload and bool(depsgraph.updates))
        )

        if needs_load:
            log(f"view_update load size={width}x{height}")

        if needs_load:
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
            bridge.load(request)
            self._viewport_loaded = True
            self._viewport_size = (width, height)
            self._viewport_reload_token = token
        else:
            camera = exporter.build_camera(
                scene,
                depsgraph,
                width / height,
                camera_matrix=region_data.view_matrix.inverted(),
                tan_half_fov=tan_half_fov,
            )
            bridge.camera(camera)

        if settings_key != getattr(self, "_viewport_settings_key", None):
            bridge.settings(settings)

        if settings.force_standard_view:
            _force_standard_view(scene)

        self._viewport_key = view_key
        self._viewport_settings_key = settings_key
        self._ensure_viewport_poll()

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
            try:
                import numpy as np
            except ImportError:
                np = None

            if np is not None:
                pixels = np.frombuffer(viewport_pixels, dtype=np.uint8).astype(np.float32) / 255.0
                pixels = pixels.reshape((-1, 4))
                rgb = pixels[:, :3]
                pixels[:, :3] = np.where(
                    rgb <= 0.04045,
                    rgb / 12.92,
                    np.power((rgb + 0.055) / 1.055, 2.4),
                )
                flat = pixels.reshape(-1)
            else:
                flat = [
                    _srgb_to_linear(value / 255.0)
                    for value in viewport_pixels
                ]

            try:
                buffer = gpu.types.Buffer("FLOAT", width * height * 4, flat)
            except (TypeError, ValueError):
                buffer = gpu.types.Buffer(
                    "FLOAT",
                    width * height * 4,
                    flat.tolist() if np is not None else flat,
                )

            texture = gpu.types.GPUTexture(
                (width, height),
                format="RGBA16F",
                data=buffer,
            )
            self._viewport_texture = texture
            if getattr(self, "_viewport_logged_size", None) != (width, height):
                self._viewport_logged_size = (width, height)
                log(f"viewport texture {width}x{height} screen-space")

        from mathutils import Matrix

        gpu.matrix.push_projection()
        gpu.matrix.load_projection_matrix(Matrix.Identity(4))
        try:
            with gpu.matrix.push_pop():
                gpu.matrix.load_identity()
                gpu.state.depth_test_set("NONE")
                gpu.state.blend_set("ALPHA_PREMULT")
                self.bind_display_space_shader(depsgraph.scene)
                draw_texture_2d(texture, (-1.0, -1.0), 2.0, 2.0)
                self.unbind_display_space_shader()
                gpu.state.blend_set("NONE")
                gpu.state.depth_test_set("LESS_EQUAL")
        finally:
            gpu.matrix.pop_projection()

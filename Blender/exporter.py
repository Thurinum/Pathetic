"""Scene export from Blender to Pathetic's headless JSON format.

Pathetic's world is right-handed and Y-up, while Blender's is right-handed and Z-up.
The camera basis is exported directly, so no global axis conversion is required: the
camera's local +X/+Y/forward vectors are the renderer's right/up/forward vectors.
"""

from __future__ import annotations

import math
from typing import Any

import bpy
from mathutils import Vector


def _to_pathetic(value) -> list[float]:
    """Blender Z-up -> Pathetic Y-up (right-handed, +Y up, -Z forward)."""
    return [float(value.x), float(value.z), -float(value.y)]


def _clamp(value: float, minimum: float = 0.0, maximum: float = 1.0) -> float:
    return max(minimum, min(maximum, value))


def _linear_byte(value: float) -> int:
    return int(round(_clamp(value) * 255.0))


def _color(rgba, strength: float = 1.0) -> list[int]:
    red = _linear_byte(float(rgba[0]) * strength)
    green = _linear_byte(float(rgba[1]) * strength)
    blue = _linear_byte(float(rgba[2]) * strength)
    alpha = _linear_byte(float(rgba[3]) if len(rgba) > 3 else 1.0)
    return [red, green, blue, alpha]


def _socket_value(node, names: tuple[str, ...]):
    for name in names:
        socket = node.inputs.get(name)
        if socket is not None:
            return socket.default_value
    return None


def _material_colors(material) -> tuple[list[int], list[int]]:
    albedo = [204, 204, 204, 255]
    emission = [0, 0, 0, 255]

    if material is None:
        return albedo, emission

    if material.use_nodes and material.node_tree is not None:
        for node in material.node_tree.nodes:
            if node.type == "BSDF_PRINCIPLED":
                base = _socket_value(node, ("Base Color",))
                if base is not None:
                    albedo = _color(base)

                emission_color = _socket_value(node, ("Emission Color", "Emission"))
                strength = _socket_value(node, ("Emission Strength",))
                if emission_color is not None:
                    emission = _color(emission_color, float(strength) if strength is not None else 1.0)
                return albedo, emission

            if node.type == "EMISSION":
                color = _socket_value(node, ("Color",))
                strength = _socket_value(node, ("Strength",))
                if color is not None:
                    emission = _color(color, float(strength) if strength is not None else 1.0)
                    albedo = [0, 0, 0, 255]
                    return albedo, emission

    diffuse = getattr(material, "diffuse_color", None)
    if diffuse is not None:
        albedo = _color(diffuse)

    return albedo, emission


def _resolution(scene) -> tuple[int, int]:
    percentage = scene.render.resolution_percentage / 100.0
    width = max(1, int(round(scene.render.resolution_x * percentage)))
    height = max(1, int(round(scene.render.resolution_y * percentage)))
    return width, height


def _vertical_tan_half_fov(camera_data, aspect: float) -> float:
    angle = float(camera_data.angle)
    fit = camera_data.sensor_fit

    if fit == "VERTICAL":
        return math.tan(angle * 0.5)

    if fit == "HORIZONTAL":
        return math.tan(angle * 0.5) / aspect

    # AUTO uses the larger dimension for the angle.
    if aspect >= 1.0:
        return math.tan(angle * 0.5) / aspect

    return math.tan(angle * 0.5)


def _camera(scene, depsgraph, aspect: float) -> dict[str, Any]:
    camera = scene.camera
    if camera is None:
        raise ValueError("Scene has no active camera.")

    evaluated = camera.evaluated_get(depsgraph)
    matrix = evaluated.matrix_world
    right = matrix.col[0].xyz.normalized()
    up = matrix.col[1].xyz.normalized()
    forward = -matrix.col[2].xyz.normalized()
    origin = matrix.translation

    focus_distance = 10.0
    aperture_radius = 0.0

    dof = camera.data.dof
    if dof.use_dof:
        focus_distance = float(dof.focus_distance)
        if dof.focus_object is not None:
            focus_distance = (dof.focus_object.matrix_world.translation - origin).length

        if dof.aperture_fstop > 0.0:
            # Blender's lens is in millimetres; Pathetic's aperture is in world units.
            aperture_radius = (float(camera.data.lens) * 0.001) / (2.0 * float(dof.aperture_fstop))

    if getattr(camera.data, "shift_x", 0.0) != 0.0 or getattr(camera.data, "shift_y", 0.0) != 0.0:
        print("[Pathetic] Camera shift is not supported and will be ignored.")

    return {
        "origin": _to_pathetic(origin),
        "right": _to_pathetic(right),
        "up": _to_pathetic(up),
        "forward": _to_pathetic(forward),
        "tanHalfFov": _vertical_tan_half_fov(camera.data, aspect),
        "focusDistance": focus_distance,
        "apertureRadius": aperture_radius,
    }


def _iter_mesh_triangles(obj, depsgraph, material_cache):
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    if mesh is None:
        return

    try:
        mesh.calc_loop_triangles()
        matrix = evaluated.matrix_world
        mirrored = matrix.to_3x3().determinant() < 0.0

        for triangle in mesh.loop_triangles:
            indices = triangle.vertices
            v0 = matrix @ mesh.vertices[indices[0]].co
            v1 = matrix @ mesh.vertices[indices[1]].co
            v2 = matrix @ mesh.vertices[indices[2]].co

            if mirrored:
                v1, v2 = v2, v1

            material = None
            if triangle.material_index < len(obj.material_slots):
                material = obj.material_slots[triangle.material_index].material

            key = material.as_pointer() if material is not None else 0
            if key not in material_cache:
                material_cache[key] = _material_colors(material)

            albedo, emission = material_cache[key]

            yield {
                "v0": _to_pathetic(v0),
                "v1": _to_pathetic(v1),
                "v2": _to_pathetic(v2),
                "albedo": albedo,
                "emission": emission,
            }
    finally:
        evaluated.to_mesh_clear()


def _iter_light_spheres(objects, settings):
    radius = float(settings.light_radius)
    for obj in objects:
        if obj.type != "LIGHT" or obj.hide_render:
            continue

        light = obj.data
        if light.type not in {"POINT", "AREA", "SPOT"}:
            continue

        position = obj.matrix_world.translation
        # Pathetic's emission is a byte value, so normalize Blender watts to a
        # useful 0..255 range. This is an approximation, not a physical mapping.
        scale = min(1.0, float(light.energy) / 1000.0)
        emission = _color(light.color, scale)

        yield {
            "center": _to_pathetic(position),
            "radius": radius,
            "albedo": [0, 0, 0, 255],
            "emission": emission,
        }


def build_request(scene, depsgraph, settings) -> dict[str, Any]:
    width, height = _resolution(scene)
    aspect = width / height

    material_cache: dict[int, tuple[list[int], list[int]]] = {}
    triangles: list[dict[str, Any]] = []
    spheres: list[dict[str, Any]] = []

    for obj in depsgraph.objects:
        if obj.hide_render:
            continue

        if obj.type == "MESH":
            triangles.extend(_iter_mesh_triangles(obj, depsgraph, material_cache))
        elif obj.type == "LIGHT":
            spheres.extend(_iter_light_spheres([obj], settings))

    return {
        "width": width,
        "height": height,
        "samples": int(settings.samples),
        "samplesPerPixel": int(settings.samples_per_frame),
        "maxBounces": int(settings.max_bounces),
        "tonemapper": str(settings.tonemapper),
        "exposure": float(settings.exposure),
        "camera": _camera(scene, depsgraph, aspect),
        "spheres": spheres,
        "planes": [],
        "triangles": triangles,
    }

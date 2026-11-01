bl_info = {
    "name": "Pathetic Renderer",
    "author": "Pathetic contributors",
    "version": (0, 1, 0),
    "blender": (4, 2, 0),
    "location": "Render Properties > Render Engine",
    "description": "Render Blender scenes with the Pathetic GPU path tracer",
    "category": "Render",
}

import bpy

from .bridge import log, stop_all
from .engine import PatheticRenderEngine
from .preferences import PatheticPreferences
from .properties import PatheticSettings


class PATHETIC_OT_reload_viewport(bpy.types.Operator):
    bl_idname = "pathetic.reload_viewport"
    bl_label = "Reload Geometry"
    bl_description = "Send the current scene geometry to the Pathetic viewport bridge"

    def execute(self, context):
        context.scene.pathetic.viewport_reload_token += 1
        return {"FINISHED"}


class PATHETIC_PT_render_settings(bpy.types.Panel):
    bl_label = "Pathetic"
    bl_space_type = "PROPERTIES"
    bl_region_type = "WINDOW"
    bl_context = "render"

    @classmethod
    def poll(cls, context):
        return context.scene.render.engine == "PATHETIC"

    def draw(self, context):
        layout = self.layout
        settings = context.scene.pathetic

        layout.prop(settings, "samples")
        layout.prop(settings, "samples_per_frame")
        layout.prop(settings, "max_bounces")
        layout.separator()
        layout.prop(settings, "tonemapper")
        layout.prop(settings, "exposure")
        layout.prop(settings, "force_standard_view")
        layout.separator()
        layout.prop(settings, "light_radius")
        layout.separator()
        layout.prop(settings, "viewport_preview")
        layout.prop(settings, "viewport_scale")
        layout.prop(settings, "viewport_samples")
        layout.prop(settings, "viewport_auto_reload")
        layout.operator("pathetic.reload_viewport", icon="FILE_REFRESH")
        layout.separator()
        layout.prop(settings, "timeout_seconds")
        layout.prop(settings, "cleanup_temp_files")
        layout.prop(settings, "keep_alive")

        layout.label(text="Pathetic supports analytic spheres, planes and triangle meshes.", icon="INFO")
        layout.label(text="Materials use Base Color and Emission only.", icon="INFO")


classes = (
    PatheticSettings,
    PatheticRenderEngine,
    PatheticPreferences,
    PATHETIC_OT_reload_viewport,
    PATHETIC_PT_render_settings,
)


def register():
    for cls in classes:
        bpy.utils.register_class(cls)

    bpy.types.Scene.pathetic = bpy.props.PointerProperty(type=PatheticSettings)
    log("pathetic_renderer registered (screen-space preview build)")


def unregister():
    stop_all()
    del bpy.types.Scene.pathetic

    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)

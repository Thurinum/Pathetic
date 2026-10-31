import bpy


class PatheticSettings(bpy.types.PropertyGroup):
    samples: bpy.props.IntProperty(
        name="Samples",
        description="Total path-tracer samples per pixel",
        default=64,
        min=1,
        max=4096,
    )
    samples_per_frame: bpy.props.IntProperty(
        name="Samples per Frame",
        description="Samples taken in each GPU dispatch. Smaller values keep dispatches short and avoid driver timeouts",
        default=4,
        min=1,
        max=64,
    )
    max_bounces: bpy.props.IntProperty(
        name="Max Bounces",
        description="Maximum number of path bounces",
        default=8,
        min=1,
        max=67,
    )
    light_radius: bpy.props.FloatProperty(
        name="Light Sphere Radius",
        description="Radius used when representing Blender lights as emissive spheres",
        default=0.15,
        min=0.0001,
        soft_max=10.0,
    )
    tonemapper: bpy.props.EnumProperty(
        name="Tonemapper",
        items=[
            ("None", "None", "No tonemapping"),
            ("Reinhard", "Reinhard", "Reinhard tonemapping"),
            ("Aces", "ACES", "ACES tonemapping"),
            ("Agx", "AgX", "AgX tonemapping"),
        ],
        default="Agx",
    )
    exposure: bpy.props.FloatProperty(
        name="Exposure",
        description="Exposure in EV applied before tonemapping",
        default=-0.3,
        soft_min=-10.0,
        soft_max=10.0,
    )
    force_standard_view: bpy.props.BoolProperty(
        name="Use Standard View Transform",
        description=(
            "Pathetic already tonemaps its present texture. Set Blender's view transform to Standard "
            "so the result is shown faithfully instead of being tonemapped a second time"
        ),
        default=True,
    )
    timeout_seconds: bpy.props.IntProperty(
        name="Timeout",
        description="Maximum time to wait for Pathetic to finish one frame",
        default=600,
        min=1,
    )
    cleanup_temp_files: bpy.props.BoolProperty(
        name="Delete Temporary Files",
        description="Delete the intermediate JSON and raw RGBA files after rendering",
        default=True,
    )
    keep_alive: bpy.props.BoolProperty(
        name="Keep Pathetic Output",
        description="Keep the JSON/RGBA files next to the Blender temp directory for debugging",
        default=False,
    )

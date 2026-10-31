import os

import bpy


class PatheticPreferences(bpy.types.AddonPreferences):
    bl_idname = __package__

    executable: bpy.props.StringProperty(
        name="Pathetic Executable",
        description="Path to Pathetic.App.exe (the headless bridge build)",
        subtype="FILE_PATH",
        default="",
    )

    def draw(self, context):
        layout = self.layout
        layout.prop(self, "executable")
        layout.label(
            text="Build with: dotnet build Pathetic.slnx -c Release",
            icon="INFO",
        )
        if not self.executable:
            layout.label(text="No executable configured yet.", icon="ERROR")
        elif not os.path.isfile(bpy.path.abspath(self.executable)):
            layout.label(text="Configured executable does not exist.", icon="ERROR")


def get_executable() -> str:
    preferences = bpy.context.preferences.addons[__package__].preferences
    configured = bpy.path.abspath(preferences.executable) if preferences.executable else ""

    if configured and os.path.isfile(configured):
        return configured

    # Development fallback: the add-on lives in <repo>/Blender, and the app builds
    # into <repo>/Pathetic.App/bin/Release/net10.0/win-x64.
    repository = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    candidates = [
        os.path.join(repository, "Pathetic.App", "bin", "Release", "net10.0", "win-x64", "Pathetic.App.exe"),
        os.path.join(repository, "Pathetic.App", "bin", "Debug", "net10.0", "win-x64", "Pathetic.App.exe"),
        os.environ.get("PATHETIC_EXE", ""),
    ]

    for candidate in candidates:
        if candidate and os.path.isfile(candidate):
            return candidate

    return configured


def get_preferences() -> PatheticPreferences:
    return bpy.context.preferences.addons[__package__].preferences

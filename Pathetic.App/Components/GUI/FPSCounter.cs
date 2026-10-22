using System.Numerics;
using ImGuiNET;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components.GUI;

public class FPSCounter : Component
{
    private const ImGuiWindowFlags OverlayFlags =
        ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.NoBackground |
        ImGuiWindowFlags.NoSavedSettings |
        ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoInputs |
        ImGuiWindowFlags.AlwaysAutoResize;

    public override void OnGUI()
    {
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.Always);
        ImGui.Begin("##fps", OverlayFlags);
        ImGui.Text($"{ImGui.GetIO().Framerate:F1} FPS");
        ImGui.End();
    }
}

using ImGuiNET;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer.Graph;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components.GUI;

public class RenderSettingsComponent(TemporalAccumulationSettings settings) : Component
{
    public override void OnGUI()
    {
        ImGui.Begin("Renderer");

        bool enabled = settings.IsEnabled;
        if (ImGui.Checkbox("Énerver Youssef", ref enabled))
            settings.IsEnabled = enabled;

        ImGui.End();
    }
}

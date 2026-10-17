using ImGuiNET;
using Microsoft.Extensions.Options;
using Pathetic.App.RenderPasses;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components.GUI;

public class RenderSettingsComponent(IOptions<TemporalAccumulationOptions> options) : Component
{
    public override void OnGUI()
    {
        ImGui.Begin("Renderer");

        bool enabled = options.Value.Enabled;
        if (ImGui.Checkbox("Temporal Accumulation", ref enabled))
            options.Value.Enabled = enabled;

        ImGui.End();
    }
}

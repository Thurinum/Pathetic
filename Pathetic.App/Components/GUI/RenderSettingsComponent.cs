using ImGuiNET;
using Microsoft.Extensions.Options;
using Pathetic.App.RenderPasses;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components.GUI;

public class RenderSettingsComponent(
    IOptions<TemporalAccumulationOptions> temporalAccumulation,
    IOptions<TonemappingOptions> tonemapping) : Component
{
    private static readonly (Tonemapper Value, string Label)[] Tonemappers =
    [
        (Tonemapper.None, "None"),
        (Tonemapper.Reinhard, "Reinhard"),
        (Tonemapper.Aces, "ACES"),
    ];

    private static readonly string[] TonemapperLabels = Tonemappers.Select(t => t.Label).ToArray();

    protected override void GUI()
    {
        ImGui.Begin("Renderer");

        bool enabled = temporalAccumulation.Value.Enabled;
        if (ImGui.Checkbox("Temporal Accumulation", ref enabled))
            temporalAccumulation.Value.Enabled = enabled;

        int tonemapper = Math.Max(0, Array.FindIndex(Tonemappers, t => t.Value == tonemapping.Value.Tonemapper));
        if (ImGui.Combo("Tonemapper", ref tonemapper, TonemapperLabels, TonemapperLabels.Length))
            tonemapping.Value.Tonemapper = Tonemappers[tonemapper].Value;

        float exposure = tonemapping.Value.Exposure;
        if (ImGui.SliderFloat("Exposure (EV)", ref exposure, -10.0f, 10.0f))
            tonemapping.Value.Exposure = exposure;

        ImGui.End();
    }
}

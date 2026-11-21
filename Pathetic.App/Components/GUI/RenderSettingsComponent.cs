using ImGuiNET;
using Microsoft.Extensions.Options;
using Pathetic.App.RenderPasses;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components.GUI;

public class RenderSettingsComponent(
    IOptions<PathTracerOptions> pathTracer,
    IOptions<TemporalAccumulationOptions> temporalAccumulation,
    IOptions<TonemappingOptions> tonemapping,
    RenderDebugSettings debug,
    ConvergenceController convergence) : Component
{
    private static readonly (Tonemapper Value, string Label)[] Tonemappers =
    [
        (Tonemapper.None, "None"),
        (Tonemapper.Reinhard, "Reinhard"),
        (Tonemapper.Agx, "AgX"),
    ];

    private static readonly string[] TonemapperLabels = Tonemappers.Select(t => t.Label).ToArray();

    protected override void GUI()
    {
        ImGui.Begin("Renderer");

        bool enabled = temporalAccumulation.Value.Enabled;
        if (ImGui.Checkbox("Temporal Accumulation", ref enabled))
            temporalAccumulation.Value.Enabled = enabled;

        int sampleBudget = (int)Math.Clamp(temporalAccumulation.Value.SampleBudget, 0u, 1_000_000u);
        if (ImGui.DragInt("Sample Budget", ref sampleBudget, 16.0f, 0, 1_000_000, sampleBudget == 0 ? "∞" : "%d"))
            temporalAccumulation.Value.SampleBudget = (uint)sampleBudget;

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("0 = auto (unlimited)");

        int movingSpp = (int)Math.Clamp(pathTracer.Value.MovingSamplesPerPixel, 1u, 4096u);
        if (ImGui.DragInt("Moving SPP", ref movingSpp, 1.0f, 1, 4096))
            pathTracer.Value.MovingSamplesPerPixel = (uint)movingSpp;

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Samples per pixel while the camera or scene is changing");

        int samplesPerPixel = (int)Math.Clamp(pathTracer.Value.SamplesPerPixel, 1u, 64u);
        if (ImGui.DragInt("SPP", ref samplesPerPixel, 1.0f, 1, 64))
        {
            pathTracer.Value.SamplesPerPixel = (uint)samplesPerPixel;
            convergence.Invalidate();
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Samples per pixel for a still frame (max 64)");

        int maxBounces = (int)Math.Clamp(pathTracer.Value.MaxBounces, 1u, 67u);
        if (ImGui.DragInt("Max Bounces", ref maxBounces, 1.0f, 1, 67))
        {
            pathTracer.Value.MaxBounces = (uint)maxBounces;
            convergence.Invalidate();
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Maximum number of path segments per sample");

        bool displayNormals = debug.DisplayNormals;
        if (ImGui.Checkbox("Display Normals", ref displayNormals))
            debug.DisplayNormals = displayNormals;

        bool smoothShading = debug.SmoothShading;
        if (ImGui.Checkbox("Smooth Shading", ref smoothShading))
            debug.SmoothShading = smoothShading;

        int tonemapper = Math.Max(0, Array.FindIndex(Tonemappers, t => t.Value == tonemapping.Value.Tonemapper));
        if (ImGui.Combo("Tonemapper", ref tonemapper, TonemapperLabels, TonemapperLabels.Length))
            tonemapping.Value.Tonemapper = Tonemappers[tonemapper].Value;

        float exposure = tonemapping.Value.Exposure;
        if (ImGui.SliderFloat("Exposure (EV)", ref exposure, -10.0f, 10.0f))
            tonemapping.Value.Exposure = exposure;

        ImGui.End();
    }
}

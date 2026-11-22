using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;
using SlangGen = Pathetic.Generated.Slang;

namespace Pathetic.App.RenderPasses;

public sealed class TemporalAccumulationPass(
    ConvergenceController convergence,
    IOptions<TonemappingOptions> tonemapping) : RenderPass
{
    private struct Params
    {
        public uint Reset;
        public uint Tonemapper;
        public float Exposure;
        public uint MaxFrames;
    }

    public override string ShaderModule => SlangGen.TemporalAccumulation.Resources.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(SlangGen.TemporalAccumulation.Resources.color, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        new TextureSpec(SlangGen.TemporalAccumulation.Resources.accumImage, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Params>(SlangGen.TemporalAccumulation.Resources.@params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        SlangGen.TemporalAccumulation.Resources.pathTracerColor,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ConvergencePlan plan = convergence.GetPlan(ctx.FrameIndex, ctx.Resources.SceneRevision);

        ctx.Cmd.UpdateBuffer(buffer, 0, new Params
        {
            Reset = plan.ResetAccumulation ? 1u : 0u,
            Tonemapper = (uint)tonemapping.Value.Tonemapper,
            Exposure = tonemapping.Value.Exposure,
            MaxFrames = plan.MaxFrames
        });
    }
}

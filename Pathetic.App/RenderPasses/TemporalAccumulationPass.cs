using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class TemporalAccumulationPass(
    ConvergenceController convergence,
    IOptions<TonemappingOptions> tonemapping) : RenderPass
{
    public override string ShaderModule => GPUType.TemporalAccumulation.Resources.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(GPUType.TemporalAccumulation.Resources.color, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        new TextureSpec(GPUType.TemporalAccumulation.Resources.accumImage, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<GPUType.TemporalAccumulation.Params>(GPUType.TemporalAccumulation.Resources.@params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        GPUType.TemporalAccumulation.Resources.pathTracerColor,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ConvergencePlan plan = convergence.GetPlan(ctx.FrameIndex, ctx.Resources.SceneRevision);

        ctx.Cmd.UpdateBuffer(buffer, 0, new GPUType.TemporalAccumulation.Params
        {
            Reset = plan.ResetAccumulation ? 1u : 0u,
            Tonemapper = (uint)tonemapping.Value.Tonemapper,
            Exposure = tonemapping.Value.Exposure,
            MaxFrames = plan.MaxFrames
        });
    }
}

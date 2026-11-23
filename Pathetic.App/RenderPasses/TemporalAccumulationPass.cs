using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class TemporalAccumulationPass(
    ConvergenceController convergence,
    IOptions<TonemappingOptions> tonemapping) : RenderPass
{
    public override string ShaderModuleName => GPUTypes.Bindings.TemporalAccumulation.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(GPUTypes.Bindings.TemporalAccumulation.color, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        new TextureSpec(GPUTypes.Bindings.TemporalAccumulation.accumImage, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<GPUTypes.TemporalAccumulationParams>(GPUTypes.Bindings.TemporalAccumulation.@params),
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ConvergencePlan plan = convergence.GetPlan(ctx.FrameIndex, ctx.Resources.SceneRevision);

        ctx.Cmd.UpdateBuffer(buffer, 0, new GPUTypes.TemporalAccumulationParams
        {
            Reset = plan.ResetAccumulation ? 1u : 0u,
            Tonemapper = (uint)tonemapping.Value.Tonemapper,
            Exposure = tonemapping.Value.Exposure,
            MaxFrames = plan.MaxFrames
        });
    }
}

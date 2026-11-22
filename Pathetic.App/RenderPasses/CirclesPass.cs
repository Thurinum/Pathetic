using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class CirclesPass : RenderPass
{
    public override string ShaderModule => GPUType.Circles.Resources.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(GPUType.Circles.Resources.circles, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<GPUType.Circles.Params>(GPUType.Circles.Resources.@params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        GPUType.Circles.Resources.circlePrimitives,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        uint count = ctx.Resources.GetElementCount(GPUType.Circles.Resources.circlePrimitives);
        ctx.Cmd.UpdateBuffer(buffer, 0, new GPUType.Circles.Params { Count = count });
    }
}
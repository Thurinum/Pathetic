using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class CirclesPass : RenderPass
{
    public override string ShaderModule => GPUType.CirclesResources.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(GPUType.CirclesResources.circles, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<GPUType.CirclesParams>(GPUType.CirclesResources.@params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        GPUType.CirclesResources.circlePrimitives,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        uint count = ctx.Resources.GetElementCount(GPUType.CirclesResources.circlePrimitives);
        ctx.Cmd.UpdateBuffer(buffer, 0, new GPUType.CirclesParams { Count = count });
    }
}
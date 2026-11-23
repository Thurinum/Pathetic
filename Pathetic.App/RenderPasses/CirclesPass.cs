using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class CirclesPass : RenderPass
{
    public override string ShaderModuleName => GPUTypes.Bindings.Circles.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(GPUTypes.Bindings.Circles.circles, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<GPUTypes.CirclesParams>(GPUTypes.Bindings.Circles.@params),
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        uint count = ctx.Resources.GetElementCount(GPUTypes.Bindings.Circles.circlePrimitives);
        ctx.Cmd.UpdateBuffer(buffer, 0, new GPUTypes.CirclesParams { Count = count });
    }
}
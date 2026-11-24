using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class CirclesPass : RenderPass
{
    public override string ShaderModuleName => Gpu.Bindings.Circles.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(Gpu.Bindings.Circles.CirclesResource, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Gpu.CirclesParams>(Gpu.Bindings.Circles.Params),
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        uint count = ctx.Resources.GetElementCount(Gpu.Bindings.Circles.CirclePrimitives);
        ctx.Cmd.UpdateBuffer(buffer, 0, new Gpu.CirclesParams { Count = count });
    }
}
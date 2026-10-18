using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class CirclesPass : RenderPass
{
    private struct Params
    {
        public uint Count;
    }

    public override string ShaderModule => "Circles";

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(ResourceName.Circles, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Params>(ResourceName.Params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        ResourceName.CirclePrimitives,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        uint count = ctx.Resources.GetElementCount(ResourceName.CirclePrimitives);
        ctx.Cmd.UpdateBuffer(buffer, 0, new Params { Count = count });
    }
}
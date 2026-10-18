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

    public override IReadOnlyList<IResourceSpec> Outputs { get; } =
    [
        new TextureSpec(Resources.Circles, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Params>(Resources.Params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        Resources.CirclePrimitives,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        Params @params = new() { Count = ctx.Resources.GetElementCount(Resources.CirclePrimitives) };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}
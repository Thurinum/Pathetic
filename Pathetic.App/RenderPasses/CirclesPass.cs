using NeoVeldrid;
using Pathetic.App.Components;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Primitives;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class CirclesPass(PrimitiveBuffer<CirclePrimitive> circles) : RenderPass
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

    public override IReadOnlyList<ResourceRef> Inputs { get; } =
    [
        new(Resources.CirclePrimitives, ResourceKind.StructuredBufferReadOnly),
    ];

    protected override void Upload(RenderContext ctx)
    {
        Params @params = new() { Count = (uint)circles.Count };
        ctx.Cmd.UpdateBuffer(ctx.Resources.GetBuffer(Resources.Params, this), 0, @params);
    }
}
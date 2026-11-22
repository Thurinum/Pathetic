using System.Runtime.InteropServices;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;
using SlangGen = Pathetic.Generated.Slang;

namespace Pathetic.App.RenderPasses;

public sealed class CirclesPass : RenderPass
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    private struct Params
    {
        public uint Count;  
    }

    public override string ShaderModule => SlangGen.Circles.Resources.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(SlangGen.Circles.Resources.circles, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Params>(SlangGen.Circles.Resources.@params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        SlangGen.Circles.Resources.circlePrimitives,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        uint count = ctx.Resources.GetElementCount(SlangGen.Circles.Resources.circlePrimitives);
        ctx.Cmd.UpdateBuffer(buffer, 0, new Params { Count = count });
    }
}
using NeoVeldrid;
using Pathetic.App.Components;
using Pathetic.Renderer.Buffers;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

// Writes a deterministic primary-hit object id buffer ("G-buffer") used by the editor selection outline.
public sealed class PrimaryIdPass(DirtyTrackingBuffer<SpherePrimitive> spheres) : RenderPass
{
    private struct Params
    {
        public uint SphereCount;
    }

    public override string ShaderModule => "ObjectId";

    public override IReadOnlyList<IResourceSpec> Outputs { get; } =
    [
        new TextureSpec(Resources.ObjectId, PixelFormat.R32_UInt, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Params>(Resources.Params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        Resources.Camera,
        Resources.Spheres,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        Params @params = new() { SphereCount = (uint)spheres.Count };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

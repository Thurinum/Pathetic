using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class PathTracerPass(IOptions<PathTracerOptions> options) : RenderPass
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Params
    {
        public uint SphereCount;
        public uint TriangleCount;
        public uint SamplesPerPixel;
        public uint MaxBounces;
        public uint BvhNodeCount;
        public uint BvhPrimitiveCount;
    }

    public override string ShaderModule => "PathTracer";

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(ResourceName.PathTracerColor, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Params>(ResourceName.Params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        ResourceName.Camera,
        ResourceName.Planes,
        ResourceName.Spheres,
        ResourceName.Triangles,
        ResourceName.BvhNodes,
        ResourceName.BvhPrimitiveRefs,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        Params @params = new()
        {
            SphereCount = ctx.Resources.GetElementCount(ResourceName.Spheres),
            TriangleCount = ctx.Resources.GetElementCount(ResourceName.Triangles),
            SamplesPerPixel = options.Value.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
            BvhNodeCount = ctx.Resources.GetElementCount(ResourceName.BvhNodes),
            BvhPrimitiveCount = ctx.Resources.GetElementCount(ResourceName.BvhPrimitiveRefs),
        };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

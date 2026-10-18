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
        public uint PlaneCount;
        public uint SphereCount;
        public uint SamplesPerPixel;
        public uint MaxBounces;
    }

    public override string ShaderModule => "PathTracer";

    public override IReadOnlyList<IResourceSpec> Outputs { get; } =
    [
        new TextureSpec(Resources.PathTracerColor, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Params>(Resources.Params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        Resources.Circles,
        Resources.Camera,
        Resources.Planes,
        Resources.Spheres,
    ];

    public override IReadOnlyList<SamplerSpec> Samplers { get; } =
    [
        new(Resources.CirclesSampler, SamplerDescription.Linear),
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        Params @params = new()
        {
            PlaneCount = ctx.Resources.GetElementCount(Resources.Planes),
            SphereCount = ctx.Resources.GetElementCount(Resources.Spheres),
            SamplesPerPixel = options.Value.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
        };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

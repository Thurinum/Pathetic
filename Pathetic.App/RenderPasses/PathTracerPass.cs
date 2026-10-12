using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.App.Components;
using Pathetic.Renderer;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Primitives;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class PathTracerPass(
    IOptions<PathTracerOptions> options,
    PrimitiveBuffer<PlanePrimitive> planes,
    PrimitiveBuffer<SpherePrimitive> spheres) : RenderPass
{
    private struct Params
    {
        public uint PlaneCount;
        public uint SphereCount;
        public uint Seed;
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

    protected override void Upload(RenderContext ctx)
    {
        Params @params = new()
        {
            PlaneCount = (uint)planes.Count,
            SphereCount = (uint)spheres.Count,
            Seed = ctx.FrameIndex,
            SamplesPerPixel = options.Value.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
        };
        ctx.Cmd.UpdateBuffer(ctx.Resources.GetBuffer(Resources.Params, this), 0, @params);
    }
}

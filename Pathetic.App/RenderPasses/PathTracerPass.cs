using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.App.Components;
using Pathetic.Renderer;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Primitives;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class PathTracerPass(IOptions<EngineOptions> options, PrimitiveRegistry primitives) : RenderPass
{
    private struct Params
    {
        public uint PlaneCount;
        public uint SphereCount;
        public uint Seed;
        public uint SamplesPerPixel;
        public uint MaxBounces;
    }

    private readonly PrimitiveBuffer<PlanePrimitive> _planes = primitives.Get<PlanePrimitive>();
    private readonly PrimitiveBuffer<SpherePrimitive> _spheres = primitives.Get<SpherePrimitive>();

    public override string ShaderModule => "PathTracer";

    public override IReadOnlyList<IResourceSpec> Outputs { get; } =
    [
        new TextureSpec("pathTracerColor", PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Params>("pathTracerParams"),
    ];

    public override IReadOnlyList<ResourceRef> Inputs { get; } =
    [
        new("circles", ResourceKind.TextureReadOnly),
        new("camera", ResourceKind.UniformBuffer),
        new("planes", ResourceKind.StructuredBufferReadOnly),
        new("spheres", ResourceKind.StructuredBufferReadOnly),
    ];

    public override IReadOnlyList<SamplerSpec> Samplers { get; } =
    [
        new("circlesSampler", SamplerDescription.Linear),
    ];

    protected override void Upload(RenderContext ctx)
    {
        Params @params = new()
        {
            PlaneCount = (uint)_planes.Count,
            SphereCount = (uint)_spheres.Count,
            Seed = ctx.FrameIndex,
            SamplesPerPixel = options.Value.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
        };
        ctx.Cmd.UpdateBuffer(ctx.Resources.GetBuffer("pathTracerParams", this), 0, @params);
    }
}

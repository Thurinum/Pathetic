using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class PathTracerPass(
    IOptions<PathTracerOptions> options,
    ConvergenceController convergence,
    RenderDebugSettings debug) : RenderPass
{
    public override string ShaderModule => GPUType.Resources.PathTracer.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(GPUType.Resources.PathTracer.pathTracerColor, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<GPUType.PathTracerParams>(GPUType.Resources.PathTracer.@params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        GPUType.Resources.PathTracer.camera,
        GPUType.Resources.PathTracer.planes,
        GPUType.Resources.PathTracer.spheres,
        GPUType.Resources.PathTracer.triangles,
        GPUType.Resources.Primitives.meshTransforms,
        GPUType.Resources.PathTracer.materials,
        GPUType.Resources.PathTracer.bvhNodes,
        GPUType.Resources.PathTracer.bvhPrimitiveRefs,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ConvergencePlan plan = convergence.GetPlan(ctx.FrameIndex, ctx.Resources.SceneRevision);
        Enabled = plan.RunPathTracer;

        GPUType.PathTracerParams @params = new()
        {
            PlaneCount = ctx.Resources.GetElementCount(GPUType.Resources.PathTracer.planes),
            SphereCount = ctx.Resources.GetElementCount(GPUType.Resources.PathTracer.spheres),
            TriangleCount = ctx.Resources.GetElementCount(GPUType.Resources.PathTracer.triangles),
            SamplesPerPixel = plan.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
            BvhNodeCount = ctx.Resources.GetElementCount(GPUType.Resources.PathTracer.bvhNodes),
            DisplayNormals = debug.DisplayNormals ? 1u : 0u,
            SmoothShading = debug.SmoothShading ? 1u : 0u,
        };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

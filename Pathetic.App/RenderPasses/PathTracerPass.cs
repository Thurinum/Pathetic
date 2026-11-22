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
    public override string ShaderModule => GPUType.PathTracer.Resources.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(GPUType.PathTracer.Resources.pathTracerColor, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<GPUType.PathTracer.Params>(GPUType.PathTracer.Resources.@params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        GPUType.PathTracer.Resources.camera,
        GPUType.PathTracer.Resources.planes,
        GPUType.PathTracer.Resources.spheres,
        GPUType.PathTracer.Resources.triangles,
        GPUType.Primitives.Resources.meshTransforms,
        GPUType.PathTracer.Resources.materials,
        GPUType.PathTracer.Resources.bvhNodes,
        GPUType.PathTracer.Resources.bvhPrimitiveRefs,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ConvergencePlan plan = convergence.GetPlan(ctx.FrameIndex, ctx.Resources.SceneRevision);
        Enabled = plan.RunPathTracer;

        GPUType.PathTracer.Params @params = new()
        {
            PlaneCount = ctx.Resources.GetElementCount(GPUType.PathTracer.Resources.planes),
            SphereCount = ctx.Resources.GetElementCount(GPUType.PathTracer.Resources.spheres),
            TriangleCount = ctx.Resources.GetElementCount(GPUType.PathTracer.Resources.triangles),
            SamplesPerPixel = plan.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
            BvhNodeCount = ctx.Resources.GetElementCount(GPUType.PathTracer.Resources.bvhNodes),
            DisplayNormals = debug.DisplayNormals ? 1u : 0u,
            SmoothShading = debug.SmoothShading ? 1u : 0u,
        };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

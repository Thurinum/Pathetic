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
    public override string ShaderModule => GPUType.PathTracerResources.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(GPUType.PathTracerResources.pathTracerColor, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<GPUType.PathTracerParams>(GPUType.PathTracerResources.@params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        GPUType.PathTracerResources.camera,
        GPUType.PathTracerResources.planes,
        GPUType.PathTracerResources.spheres,
        GPUType.PathTracerResources.triangles,
        GPUType.PrimitivesResources.meshTransforms,
        GPUType.PathTracerResources.materials,
        GPUType.PathTracerResources.bvhNodes,
        GPUType.PathTracerResources.bvhPrimitiveRefs,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ConvergencePlan plan = convergence.GetPlan(ctx.FrameIndex, ctx.Resources.SceneRevision);
        Enabled = plan.RunPathTracer;

        GPUType.PathTracerParams @params = new()
        {
            PlaneCount = ctx.Resources.GetElementCount(GPUType.PathTracerResources.planes),
            SphereCount = ctx.Resources.GetElementCount(GPUType.PathTracerResources.spheres),
            TriangleCount = ctx.Resources.GetElementCount(GPUType.PathTracerResources.triangles),
            SamplesPerPixel = plan.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
            BvhNodeCount = ctx.Resources.GetElementCount(GPUType.PathTracerResources.bvhNodes),
            DisplayNormals = debug.DisplayNormals ? 1u : 0u,
            SmoothShading = debug.SmoothShading ? 1u : 0u,
        };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

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
    public override string ShaderModuleName => GPUTypes.Bindings.PathTracer.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(GPUTypes.Bindings.PathTracer.pathTracerColor, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<GPUTypes.PathTracerParams>(GPUTypes.Bindings.PathTracer.@params),
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ConvergencePlan plan = convergence.GetPlan(ctx.FrameIndex, ctx.Resources.SceneRevision);
        Enabled = plan.RunPathTracer;

        GPUTypes.PathTracerParams @params = new()
        {
            PlaneCount = ctx.Resources.GetElementCount(GPUTypes.Bindings.PathTracer.planes),
            SphereCount = ctx.Resources.GetElementCount(GPUTypes.Bindings.PathTracer.spheres),
            TriangleCount = ctx.Resources.GetElementCount(GPUTypes.Bindings.PathTracer.triangles),
            SamplesPerPixel = plan.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
            BvhNodeCount = ctx.Resources.GetElementCount(GPUTypes.Bindings.PathTracer.bvhNodes),
            DisplayNormals = debug.DisplayNormals ? 1u : 0u,
            SmoothShading = debug.SmoothShading ? 1u : 0u,
        };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

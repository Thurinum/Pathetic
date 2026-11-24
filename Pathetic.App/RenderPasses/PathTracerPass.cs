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
    public override string ShaderModuleName => Gpu.Bindings.PathTracer.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(Gpu.Bindings.PathTracer.PathTracerColor, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Gpu.PathTracerParams>(Gpu.Bindings.PathTracer.Params),
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ConvergencePlan plan = convergence.GetPlan(ctx.FrameIndex, ctx.Resources.SceneRevision);
        Enabled = plan.RunPathTracer;

        Gpu.PathTracerParams @params = new()
        {
            PlaneCount = ctx.Resources.GetElementCount(Gpu.Bindings.PathTracer.Planes),
            SphereCount = ctx.Resources.GetElementCount(Gpu.Bindings.PathTracer.Spheres),
            TriangleCount = ctx.Resources.GetElementCount(Gpu.Bindings.PathTracer.Triangles),
            SamplesPerPixel = plan.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
            BvhNodeCount = ctx.Resources.GetElementCount(Gpu.Bindings.PathTracer.BvhNodes),
            DisplayNormals = debug.DisplayNormals ? 1u : 0u,
            SmoothShading = debug.SmoothShading ? 1u : 0u,
        };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

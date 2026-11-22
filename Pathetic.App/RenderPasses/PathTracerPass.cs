using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;
using SlangGen = Pathetic.Generated.Slang;

namespace Pathetic.App.RenderPasses;

public sealed class PathTracerPass(
    IOptions<PathTracerOptions> options,
    ConvergenceController convergence,
    RenderDebugSettings debug) : RenderPass
{
    public override string ShaderModule => SlangGen.PathTracer.Resources.ModuleName;

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(SlangGen.PathTracer.Resources.pathTracerColor, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<SlangGen.PathTracer.Params>(SlangGen.PathTracer.Resources.@params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        SlangGen.PathTracer.Resources.camera,
        SlangGen.PathTracer.Resources.planes,
        SlangGen.PathTracer.Resources.spheres,
        SlangGen.PathTracer.Resources.triangles,
        SlangGen.Primitives.Resources.meshTransforms,
        SlangGen.PathTracer.Resources.materials,
        SlangGen.PathTracer.Resources.bvhNodes,
        SlangGen.PathTracer.Resources.bvhPrimitiveRefs,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ConvergencePlan plan = convergence.GetPlan(ctx.FrameIndex, ctx.Resources.SceneRevision);
        Enabled = plan.RunPathTracer;

        SlangGen.PathTracer.Params @params = new()
        {
            PlaneCount = ctx.Resources.GetElementCount(SlangGen.PathTracer.Resources.planes),
            SphereCount = ctx.Resources.GetElementCount(SlangGen.PathTracer.Resources.spheres),
            TriangleCount = ctx.Resources.GetElementCount(SlangGen.PathTracer.Resources.triangles),
            SamplesPerPixel = plan.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
            BvhNodeCount = ctx.Resources.GetElementCount(SlangGen.PathTracer.Resources.bvhNodes),
            DisplayNormals = debug.DisplayNormals ? 1u : 0u,
            SmoothShading = debug.SmoothShading ? 1u : 0u,
        };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

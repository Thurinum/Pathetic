using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class PathTracerPass(
    IOptions<PathTracerOptions> options,
    ConvergenceController convergence) : RenderPass
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Params
    {
        public uint PrimitiveCount;
        public uint SamplesPerPixel;
        public uint MaxBounces;
        public uint BvhNodeCount;
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
        ResourceName.Primitives,
        ResourceName.Materials,
        ResourceName.SphereData,
        ResourceName.PlaneData,
        ResourceName.BvhNodes,
        ResourceName.BvhPrimitiveRefs,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ConvergencePlan plan = convergence.GetPlan(ctx.FrameIndex, ctx.Resources.SceneRevision);
        Enabled = plan.RunPathTracer;

        Params @params = new()
        {
            PrimitiveCount = ctx.Resources.GetElementCount(ResourceName.Primitives),
            SamplesPerPixel = plan.SamplesPerPixel,
            MaxBounces = options.Value.MaxBounces,
            BvhNodeCount = ctx.Resources.GetElementCount(ResourceName.BvhNodes),
        };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

using NeoVeldrid;
using Pathetic.Renderer.Buffers;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class PrimaryRayPass(Ray request, GpuReadback<PrimaryHit> readback) : RenderPass
{
    public override string ShaderModule => "PrimaryRay";

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        UniformBufferSpec.Of<PrimaryRayParams>(ResourceName.PrimaryRayParams)
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        ResourceName.Planes,
        ResourceName.Spheres,
    ];

    protected override (uint x, uint y) DispatchSize(RenderContext ctx) => (1, 1);

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        PrimaryRayParams data = new()
        {
            Origin = request.Origin,
            Direction = request.Direction,
            PlaneCount = ctx.Resources.GetElementCount(ResourceName.Planes),
            SphereCount = ctx.Resources.GetElementCount(ResourceName.Spheres),
        };
        ctx.Cmd.UpdateBuffer(buffer, 0, data);
    }

    protected override void PostExecute(RenderContext ctx, PassRenderState state)
    {
        readback.RecordCopy(ctx.Cmd);
    }
}

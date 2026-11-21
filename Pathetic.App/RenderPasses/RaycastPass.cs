using NeoVeldrid;
using Pathetic.Renderer.Buffers;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class RaycastPass(RaycastService service, GpuReadback<HitResult> readback) : RenderPass
{
    public override string ShaderModule => "Raycast";

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        UniformBufferSpec.Of<RaycastParams>(ResourceName.RaycastParams)
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        ResourceName.Planes,
        ResourceName.Spheres,
        ResourceName.Triangles,
        ResourceName.MeshTransforms,
    ];

    protected override (uint x, uint y) DispatchSize(RenderContext ctx) => (1, 1);

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        // Publish the previous frame's hit before arming this frame's copy.
        service.Poll();

        Ray request = service.Request;
        RaycastParams data = new()
        {
            Origin = request.Origin,
            Direction = request.Direction,
            PlaneCount = ctx.Resources.GetElementCount(ResourceName.Planes),
            SphereCount = ctx.Resources.GetElementCount(ResourceName.Spheres),
            TriangleCount = ctx.Resources.GetElementCount(ResourceName.Triangles),
        };
        ctx.Cmd.UpdateBuffer(buffer, 0, data);
    }

    protected override void PostExecute(RenderContext ctx, PassRenderState state)
    {
        if (!service.AwaitingResult)
            return;

        readback.RecordCopy(ctx.Cmd);
    }
}

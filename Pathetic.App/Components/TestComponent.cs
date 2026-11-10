using System.Numerics;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components;

public sealed class TestComponent(
    Ray request,
    GpuReadback<PrimaryHit> readback) : Component
{
    private static readonly Vector3 Origin = new(0.0f, 5.0f, 10.0f);
    private static readonly Vector3 Target = new(2.0f, 0.2f, 0.0f);

    private int _frames;

    protected override void Update(float deltaTime)
    {
        request.Origin = Origin;
        request.Direction = Vector3.Normalize(Target - Origin);

        readback.Wait();
        if (!readback.TryRead(out PrimaryHit hit))
            return;

        if (++_frames % 60 != 0)
            return;

        Console.WriteLine(hit.IsHit
            ? $"[primary ray] t={hit.T:F3} primitive={hit.Primitive} index={hit.Index}"
            : "[primary ray] miss");
    }
}

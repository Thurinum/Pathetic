using System.Numerics;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer.Buffers;

namespace Pathetic.App;

public sealed class RaycastService(GpuReadback<PrimaryHit> readback)
{
    private Ray _request = new(Vector3.Zero, -Vector3.UnitZ);
        
    internal Ray Request => _request;
    internal bool AwaitingResult { get; private set; }
    
    public event Action<PrimaryHit>? ResultReceived;

    public void Raycast(Ray ray)
    {
        _request.Origin = ray.Origin;
        _request.Direction = ray.Direction;
        AwaitingResult = true;
    }
    
    internal void Poll()
    {
        readback.Wait();

        if (!readback.TryRead(out PrimaryHit hit))
            return;

        AwaitingResult = false;
        ResultReceived?.Invoke(hit);
    }
}

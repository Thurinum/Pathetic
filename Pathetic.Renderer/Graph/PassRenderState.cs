using NeoVeldrid;
using Pathetic.Renderer.Utils;

namespace Pathetic.Renderer.Graph;

public sealed class PassRenderState : IDisposable
{
    private bool _disposed;

    public required ResourceLayout Layout { get; init; }
    public required ResourceLayoutElementDescription[] Bindings { get; init; }
    public ResourceSet? Set { get; set; }
    public required Pipeline Pipeline { get; init; }
    public required UIntVector3 ThreadGroupSize { get; init; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        Pipeline.Dispose();
        Set?.Dispose();
        Layout.Dispose();
    }
}
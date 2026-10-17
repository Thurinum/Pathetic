using Microsoft.Extensions.Options;
using Pathetic.Renderer.Graph;

namespace Pathetic.Renderer;

public sealed class RenderGraphHost(RenderWindow window, IOptions<RenderGraphSpec> graphSpec, RenderGraphFactory graphFactory) : IDisposable
{
    private RenderGraph? _graph;
    private bool _disposed;

    public void Initialize()
    {
        _graph = graphFactory.Create(graphSpec.Value, window.Device);
        window.Resized += OnWindowResized;
    }

    public void Render(float deltaTime)
    {
        _graph!.Render(window.Device, window.CommandList, deltaTime);
    }

    private void OnWindowResized()
    {
        _graph?.Resize(window.Device, (uint)window.Width, (uint)window.Height);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _graph?.Dispose();
    }
}

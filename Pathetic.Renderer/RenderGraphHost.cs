using Microsoft.Extensions.Options;
using Pathetic.Renderer.Graph;

namespace Pathetic.Renderer;

public sealed class RenderGraphHost(RenderWindow window, IOptions<RenderGraphSpec> graphSpec, RenderGraphFactory graphFactory) : IDisposable
{
    private RenderGraph Graph
    {
        get => field ?? throw new InvalidOperationException($"{nameof(RenderGraphHost)} is not initialized.");
        set;
    }

    private bool _disposed;
    private bool _initialized;

    public void Initialize()
    {
        Graph = graphFactory.Create(graphSpec.Value, window.Device);
        window.Resized += OnWindowResized;
        _initialized = true;
    }

    public void Render(float deltaTime)
    {
        Graph.Render(window.Device, window.CommandList, deltaTime);
    }

    private void OnWindowResized()
    {
        Graph.Resize(window.Device, (uint)window.Width, (uint)window.Height);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!_initialized)
            return;

        window.Device.WaitForIdle();
        Graph.Dispose();
    }
}

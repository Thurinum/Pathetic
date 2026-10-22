using NeoVeldrid;

namespace Pathetic.Renderer;

public sealed class ImGuiHost(RenderWindow window) : IDisposable
{
    private ImGuiRenderer? _imgui;
    private bool _disposed;

    public void Initialize()
    {
        var device = window.Device;

        _imgui = new ImGuiRenderer(
            device,
            device.MainSwapchain.Framebuffer.OutputDescription,
            window.Width,
            window.Height);

        window.Resized += OnWindowResized;
    }

    public void Begin(float deltaTime, InputSnapshot input)
    {
        _imgui!.Update(deltaTime, input);
    }

    public void End()
    {
        _imgui!.Render(window.Device, window.CommandList);
    }

    private void OnWindowResized()
    {
        _imgui?.WindowResized(window.Width, window.Height);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _imgui?.Dispose();
    }
}

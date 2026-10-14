using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using NeoVeldrid.Sdl2;
using NeoVeldrid.StartupUtilities;

namespace Pathetic.Renderer;

public sealed partial class RenderWindow(IOptions<RendererOptions> options, ILogger<RenderWindow> logger) : IDisposable
{
    private Sdl2Window? _window;
    private GraphicsDevice? _device;
    private CommandList? _cmd;
    private bool _disposed;

    public bool Exists => _window?.Exists ?? false;
    public int Width => _window?.Width ?? 0;
    public int Height => _window?.Height ?? 0;
    public GraphicsDevice Device => _device ?? throw new InvalidOperationException("RenderWindow is not initialized.");
    public CommandList CommandList => _cmd ?? throw new InvalidOperationException("RenderWindow is not initialized.");
    public event Action? Resized;

    public void Initialize()
    {
        var config = options.Value;
        
        WindowCreateInfo windowDescription = new()
        {
            X = config.X,
            Y = config.Y,
            WindowWidth = (int)config.WindowWidth,
            WindowHeight = (int)config.WindowHeight,
            WindowTitle = config.WindowTitle
        };

        GraphicsDeviceOptions deviceOptions = new()
        {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true,
            SyncToVerticalBlank = config.VSync,
        };

        NeoVeldridStartup.CreateWindowAndGraphicsDevice
        (
            windowDescription,
            deviceOptions,
            GraphicsBackend.Vulkan,
            out _window,
            out _device
        );
        
        LogDevice(logger, _device.DeviceName, _device.VendorName);

        _window.Resized += OnWindowResized;
        _cmd = _device.ResourceFactory.CreateCommandList();
    }

    public InputSnapshot PumpEvents()
    {
        return _window!.PumpEvents();
    }

    public void BeginFrame()
    {
        CommandList.Begin();
        CommandList.SetFramebuffer(Device.SwapchainFramebuffer); // for imgui
    }

    public void EndFrame()
    {
        CommandList.End();
        Device.SubmitCommands(CommandList);
        Device.SwapBuffers(Device.MainSwapchain);
    }

    private void OnWindowResized()
    {
        Device.ResizeMainWindow((uint)Width, (uint)Height);
        Resized?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_device is null)
            return;

        _device.WaitForIdle();
        _cmd?.Dispose();
        _device.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Device: {DeviceName} ({VendorName})")]
    private static partial void LogDevice(ILogger logger, string deviceName, string vendorName);
}

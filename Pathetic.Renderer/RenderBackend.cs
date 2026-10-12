using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using NeoVeldrid.Sdl2;
using NeoVeldrid.StartupUtilities;
using Pathetic.Renderer.Graph;

namespace Pathetic.Renderer;

public sealed class RenderBackend : IDisposable
{
    private readonly Sdl2Window _window;
    private readonly GraphicsDevice _device;
    private readonly CommandList _cmd;
    private readonly ImGuiRenderer _imgui;
    private readonly RenderGraph _graph;
    public bool Shown => _window.Exists;

    public RenderBackend(
        IOptions<RendererOptions> options,
        RenderGraph graph,
        ILogger<RenderBackend> logger)
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
        
        logger.LogInformation($"Device: {_device.DeviceName} ({_device.VendorName})");

        _window.Resized += OnWindowResized;
        _cmd = _device.ResourceFactory.CreateCommandList();
        _imgui = new ImGuiRenderer(
            _device,
            _device.MainSwapchain.Framebuffer.OutputDescription,
            _window.Width,
            _window.Height);

        _graph = graph;
        _graph.Build(_device);
    }
    
    public InputSnapshot GetInput()
    {
        return _window.PumpEvents();
    }

    public void BeginFrame()
    {
        _cmd.Begin();
        _cmd.SetFramebuffer(_device.SwapchainFramebuffer); // for imgui
    }

    public void Render(float deltaTime)
    {
        _graph.Render(_device, _cmd, deltaTime);
    }

    public void EndFrame()
    {
        _cmd.End();
        _device.SubmitCommands(_cmd);
        _device.SwapBuffers(_device.MainSwapchain);
    }
    
    public void BeginGUI(float deltaTime, InputSnapshot input)
    {
        _imgui.Update(deltaTime, input);
    }

    public void EndGUI()
    {
        _imgui.Render(_device, _cmd);
    }

    private void OnWindowResized()
    {
        _device.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
        _graph.Resize(_device, (uint)_window.Width, (uint)_window.Height);
        _imgui.WindowResized(_window.Width, _window.Height);
    }

    public void Dispose()
    {
        _device.WaitForIdle();
        _imgui.Dispose();
        _cmd.Dispose();
        _graph.Dispose();
        _device.Dispose();
    }
}

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NeoVeldrid;
using Pathetic.Profiling.Statistics;

namespace Pathetic.Profiling.Gpu;

public sealed partial class FenceGpuTimer(
    ILogger<FenceGpuTimer> logger,
    ShaderCounterStats counterStats,
    IEnumerable<IShaderCounterReadback> counterReadbacks) : IGpuTimer, IDisposable
{
    private static readonly ActivitySource Source = new(ProfilingName.SourceName);

    private Fence? _fence;

    public void Lap(GraphicsDevice device, CommandList cmd, string label)
    {
        if (_fence is null)
        {
            _fence = device.ResourceFactory.CreateFence(false);
            LogDevice(device);
        }

        DateTimeOffset start = DateTimeOffset.UtcNow;
        long startTicks = TimeProvider.System.GetTimestamp();
        foreach (IShaderCounterReadback readback in counterReadbacks)
            readback.Record(device, cmd);

        cmd.End();
        device.SubmitCommands(cmd, _fence);
        device.WaitForFence(_fence);
        _fence.Reset();
        TimeSpan elapsed = TimeProvider.System.GetElapsedTime(startTicks);

        foreach (IShaderCounterReadback readback in counterReadbacks)
            counterStats.Update(readback.Resolve(device), elapsed);

        using Activity? span = Source.StartActivity(
            ProfilingName.GpuSpanPrefix + label, ActivityKind.Internal, parentContext: default, startTime: start);
        span?.SetEndTime((start + elapsed).UtcDateTime);

        cmd.Begin();
        cmd.SetFramebuffer(device.SwapchainFramebuffer);
    }

    private void LogDevice(GraphicsDevice device)
    {
        string driver = device.GetVulkanInfo(out BackendInfoVulkan vulkan) ? $"{vulkan.DriverName} {vulkan.DriverInfo}" : string.Empty;
        LogDevice(device.DeviceName, device.VendorName, device.BackendType, device.ApiVersion, device.IsDebugActive, driver);
    }

    public void Dispose() => _fence?.Dispose();

    [LoggerMessage(Level = LogLevel.Information, Message = "GPU {Name} ({Vendor}), {Backend} {Api}, validation {Validation}, driver {Driver}")]
    private partial void LogDevice(string name, string vendor, GraphicsBackend backend, GraphicsApiVersion api, bool validation, string driver);
}

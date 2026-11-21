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
    private const string FinalLabel = "Present";

    private static readonly ActivitySource Source = new(ProfilingName.SourceName);

    private readonly List<Submission> _submissions = [];
    private readonly Stack<Fence> _idle = new();
    private readonly List<Fence> _all = [];

    private bool _deviceLogged;
    private DateTimeOffset _frameStart;

    public void Record(GraphicsDevice device, CommandList cmd, string label)
    {
        if (!_deviceLogged)
        {
            _deviceLogged = true;
            LogDevice(device);
        }

        if (_submissions.Count == 0)
            _frameStart = DateTimeOffset.UtcNow;

        Fence fence = RentFence(device);
        long submittedAt = TimeProvider.System.GetTimestamp();

        cmd.End();
        device.SubmitCommands(cmd, fence);
        _submissions.Add(new Submission(label, fence, submittedAt));

        cmd.Begin();
        cmd.SetFramebuffer(device.SwapchainFramebuffer);
    }

    public void Publish(GraphicsDevice device, CommandList cmd)
    {
        if (_submissions.Count == 0)
            return;

        foreach (IShaderCounterReadback readback in counterReadbacks)
            readback.Record(device, cmd);

        Record(device, cmd, FinalLabel);

        long frameStart = _submissions[0].SubmittedAt;
        long previousDone = frameStart;

        foreach (Submission submission in _submissions)
        {
            device.WaitForFence(submission.Fence);
            long done = TimeProvider.System.GetTimestamp();

            // The GPU runs submissions in order, so a workload starts when the previous one ends or when it was submitted.
            long began = Math.Max(previousDone, submission.SubmittedAt);
            EmitSpan(submission.Label, frameStart, began, done);

            previousDone = done;
            submission.Fence.Reset();
            _idle.Push(submission.Fence);
        }

        TimeSpan gpuTime = TimeProvider.System.GetElapsedTime(frameStart, previousDone);
        _submissions.Clear();

        foreach (IShaderCounterReadback readback in counterReadbacks)
            counterStats.Update(readback.Resolve(device), gpuTime);
    }

    private void EmitSpan(string label, long frameStart, long began, long done)
    {
        DateTimeOffset start = _frameStart + TimeProvider.System.GetElapsedTime(frameStart, began);
        TimeSpan duration = TimeProvider.System.GetElapsedTime(began, done);

        using Activity? span = Source.StartActivity(
            ProfilingName.GpuSpanPrefix + label, ActivityKind.Internal, parentContext: default, startTime: start);
        span?.SetEndTime((start + duration).UtcDateTime);
    }

    private Fence RentFence(GraphicsDevice device)
    {
        if (_idle.TryPop(out Fence? fence))
            return fence;

        fence = device.ResourceFactory.CreateFence(false);
        _all.Add(fence);
        return fence;
    }

    private void LogDevice(GraphicsDevice device)
    {
        string driver = device.GetVulkanInfo(out BackendInfoVulkan vulkan) ? $"{vulkan.DriverName} {vulkan.DriverInfo}" : string.Empty;
        LogDevice(device.DeviceName, device.VendorName, device.BackendType, device.ApiVersion, device.IsDebugActive, driver);
    }

    public void Dispose()
    {
        foreach (Fence fence in _all)
            fence.Dispose();
    }

    private readonly record struct Submission(string Label, Fence Fence, long SubmittedAt);

    [LoggerMessage(Level = LogLevel.Information, Message = "GPU {Name} ({Vendor}), {Backend} {Api}, validation {Validation}, driver {Driver}")]
    private partial void LogDevice(string name, string vendor, GraphicsBackend backend, GraphicsApiVersion api, bool validation, string driver);
}

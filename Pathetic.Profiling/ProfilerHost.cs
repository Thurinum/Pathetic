using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pathetic.Profiling.Statistics;

namespace Pathetic.Profiling;

internal sealed partial class ProfilerHost : IHostedService, IDisposable
{
    private const int SummarySpanCount = 8;
    private const int TaggedSpanLogLimit = 3;

    private static readonly TimeSpan SummaryInterval = TimeSpan.FromSeconds(10);

    private readonly ProfilerOptions _options;
    private readonly ProfilerStats _stats;
    private readonly ShaderCounterStats _shaderCounters;
    private readonly ILogger<ProfilerHost> _logger;
    private readonly ActivityListener _listener;

    private DateTime? _previousFrameStart;
    private long _lastSummary = TimeProvider.System.GetTimestamp();

    public ProfilerHost(IOptions<ProfilerOptions> options, ProfilerStats stats, ShaderCounterStats shaderCounters, ILogger<ProfilerHost> logger)
    {
        _options = options.Value;
        _stats = stats;
        _shaderCounters = shaderCounters;
        _logger = logger;
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith(ProfilingName.SourcePrefix, StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = OnSpanStopped,
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.Enabled)
            ActivitySource.AddActivityListener(_listener);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void OnSpanStopped(Activity span)
    {
        string name = span.OperationName;
        string tags = FormatTags(span);

        long occurrences = _stats.Add(name, span.Duration.TotalMilliseconds, tags);

        if (tags.Length > 0 && occurrences <= TaggedSpanLogLimit)
            LogTaggedSpan(name, span.Duration.TotalMilliseconds, tags);

        if (span.GetTagItem(ProfilingName.FrameTag) is not null)
            OnFrameStarted(span.StartTimeUtc);
    }

    private static string FormatTags(Activity span) =>
        string.Join(' ', span.TagObjects
            .Where(tag => tag.Key != ProfilingName.FrameTag)
            .Select(tag => $"{tag.Key}={tag.Value}"));

    private void OnFrameStarted(DateTime start)
    {
        if (_previousFrameStart is { } previous)
            _stats.AddFrame((start - previous).TotalMilliseconds);

        _previousFrameStart = start;

        if (TimeProvider.System.GetElapsedTime(_lastSummary) >= SummaryInterval)
            LogSummaryNow();
    }

    private void LogSummaryNow()
    {
        _lastSummary = TimeProvider.System.GetTimestamp();
        if (!_logger.IsEnabled(LogLevel.Information))
            return;

        ProfilerSnapshot snapshot = _stats.Snapshot();
        IEnumerable<string> counters = _shaderCounters.Rows().Select(row => $"  {row.Name,-44} {row.Value}");
        IEnumerable<string> spans = snapshot.Spans.Take(SummarySpanCount)
            .Select(span => $"  {span.Name,-44} {span.MeanMs,9:F3} ms  max {span.MaxMs,9:F3}");
        IEnumerable<string> state = snapshot.Spans.Where(span => span.Tags.Length > 0).Select(span => $"  {span.Name,-44} {span.Tags}");
        string rows = string.Join(Environment.NewLine, counters.Concat(spans).Concat(state));
        LogSummary(snapshot.Fps, snapshot.FrameMeanMs, rows);
    }

    public void Dispose() => _listener.Dispose();

    [LoggerMessage(Level = LogLevel.Information, Message = "{Span}: {Ms:F2} ms  {Tags}")]
    private partial void LogTaggedSpan(string span, double ms, string tags);

    [LoggerMessage(Level = LogLevel.Information, Message = "Profiler {Fps:F1} fps, frame {FrameMs:F2} ms\n{Rows}")]
    private partial void LogSummary(double fps, double frameMs, string rows);
}

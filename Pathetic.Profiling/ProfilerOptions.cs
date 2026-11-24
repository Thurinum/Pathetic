using System.ComponentModel.DataAnnotations;

namespace Pathetic.Profiling;

public sealed record ProfilerOptions
{
    private const int DefaultWindowFrames = 240;
    private const int MaxWindowFrames = 100_000;

    public const string Section = "Profiler";

    /// <summary>Number of most recent frames that every statistic is computed over.</summary>
    [Range(1, MaxWindowFrames)]
    public int WindowFrames { get; init; } = DefaultWindowFrames;

    /// <summary>When set, every span is written to this file as Chrome Trace Event JSON (chrome://tracing, ui.perfetto.dev).</summary>
    public string TraceFile { get; init; } = string.Empty;
}

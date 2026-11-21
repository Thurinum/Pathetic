namespace Pathetic.Profiling.Statistics;

public readonly record struct SpanTag(string Key, string Value);

public sealed record SpanStats(
    string Name,
    double LastMs,
    double MeanMs,
    double MaxMs,
    double CallsPerFrame,
    IReadOnlyList<SpanTag> Tags,
    bool IsEvent)
{
    public double TotalMsPerFrame => MeanMs * CallsPerFrame;

    public bool IsGpu => Name.StartsWith(ProfilingName.GpuSpanPrefix, StringComparison.Ordinal);
}

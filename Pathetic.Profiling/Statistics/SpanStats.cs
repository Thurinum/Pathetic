namespace Pathetic.Profiling.Statistics;

public sealed record SpanStats(
    string Name,
    double LastMs,
    double MeanMs,
    double MaxMs,
    double CallsPerFrame,
    string Tags,
    TimeSpan Age)
{
    public static readonly TimeSpan EventThreshold = TimeSpan.FromSeconds(2);

    public double TotalMsPerFrame => MeanMs * CallsPerFrame;

    public bool IsGpu => Name.StartsWith(ProfilingName.GpuSpanPrefix, StringComparison.Ordinal);

    public bool IsEvent => Age > EventThreshold;
}

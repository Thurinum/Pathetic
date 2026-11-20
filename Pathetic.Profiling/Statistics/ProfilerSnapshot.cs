namespace Pathetic.Profiling.Statistics;

public sealed record ProfilerSnapshot(
    double Fps,
    double FrameMeanMs,
    float[] FrameHistoryMs,
    IReadOnlyList<SpanStats> Spans,
    IReadOnlyList<SpanStats> Events);

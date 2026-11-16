namespace Pathetic.Profiling;

public sealed record ProfilerOptions
{
    public const string Section = "Profiler";

    public bool Enabled { get; init; }
}

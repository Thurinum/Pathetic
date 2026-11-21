namespace Pathetic.Profiling.Gpu;

/// <summary>Describes the shader counters. Implementations are generated from the shader sources.</summary>
public interface IShaderCounterLayout
{
    int Count { get; }

    IEnumerable<(string Name, string Value)> Macros { get; }
}

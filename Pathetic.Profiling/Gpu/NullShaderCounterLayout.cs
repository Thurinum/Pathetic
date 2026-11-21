namespace Pathetic.Profiling.Gpu;

public sealed class NullShaderCounterLayout : IShaderCounterLayout
{
    public int Count => 0;

    public IEnumerable<(string Name, string Value)> Macros => [];
}

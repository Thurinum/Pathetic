using Pathetic.Profiling.Statistics;

namespace Pathetic.Profiling.Gpu;

public sealed class NullShaderCounterSource : IShaderCounterSource
{
    public string Pass => string.Empty;

    public uint SizeInBytes => sizeof(uint);

    public IReadOnlyList<ShaderCounterRow> Rows(ReadOnlySpan<byte> counters, TimeSpan passTime) => [];
}

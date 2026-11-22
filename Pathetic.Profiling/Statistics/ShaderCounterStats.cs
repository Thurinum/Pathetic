using Pathetic.Profiling.Gpu;

namespace Pathetic.Profiling.Statistics;

public readonly record struct ShaderCounterRow(string Name, string Value);

public sealed class ShaderCounterStats(IShaderCounterSource source)
{
    private byte[] _values = [];
    private TimeSpan _passTime;

    internal void Update(byte[] values, IReadOnlyDictionary<string, TimeSpan> passTimes)
    {
        if (values.Length == 0)
            return;

        _values = values;
        _passTime = passTimes.GetValueOrDefault(source.Pass);
    }

    public IReadOnlyList<ShaderCounterRow> Rows() => _values.Length == 0 ? [] : source.Rows(_values, _passTime);
}

namespace Pathetic.Profiling.Statistics;

public readonly record struct ShaderCounterRow(string Name, string Value);

/// <summary>
/// Latest shader counter values. Derive from it and mark properties with
/// <see cref="Aspects.ShaderCounterAttribute"/> to have <see cref="Rows"/> generated.
/// </summary>
public abstract class ShaderCounterStats
{
    private uint[] _values = [];

    protected TimeSpan GpuTime { get; private set; }

    protected bool HasValues => _values.Length > 0;

    internal void Update(uint[] values, TimeSpan gpuTime)
    {
        if (values.Length == 0)
            return;

        _values = values;
        GpuTime = gpuTime;
    }

    public abstract IReadOnlyList<ShaderCounterRow> Rows();

    protected double Get<TCounter>(TCounter counter) where TCounter : struct, Enum => _values[Convert.ToInt32(counter, System.Globalization.CultureInfo.InvariantCulture)];

    protected static double Ratio(double value, double total) => total > 0 ? value / total : 0;
}

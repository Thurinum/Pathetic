using Pathetic.Profiling.Gpu;

namespace Pathetic.Profiling.Statistics;

public readonly record struct ShaderCounterRow(string Name, string Value);

public sealed class ShaderCounterStats
{
    private const double RaysPerMegaray = 1_000_000;

    // The counters are written by this pass, so rates are measured against its own GPU time.
    private const string CountingPass = "PathTracerPass";

    private uint[] _values = [];
    private TimeSpan _gpuTime;

    internal void Update(uint[] values, IReadOnlyDictionary<string, TimeSpan> passTimes)
    {
        if (values.Length == 0)
            return;

        _values = values;
        _gpuTime = passTimes.GetValueOrDefault(CountingPass);
    }

    public IReadOnlyList<ShaderCounterRow> Rows()
    {
        if (_values.Length == 0)
            return [];

        double paths = Get(ShaderCounter.Paths);
        double rays = Get(ShaderCounter.Rays);

        return
        [
            new("Paths per frame", $"{paths:N0}"),
            new("Rays per frame", $"{rays:N0}"),
            new("Rays per path", $"{Ratio(rays, paths):F2}"),
            new("Throughput", $"{Ratio(rays, _gpuTime.TotalSeconds) / RaysPerMegaray:F0} Mrays/s"),
            new("Rays by bounce (0 / 1 / 2 / 3+)", RaysByBounce(rays)),
            new("Rays missing the BVH root", $"{Ratio(Get(ShaderCounter.RootMisses), rays):P1}"),
            new("BVH nodes visited per ray", $"{Ratio(Get(ShaderCounter.NodeVisits), rays):F1}"),
            new("SIMD efficiency in traversal", $"{Ratio(Get(ShaderCounter.NodeVisits), Get(ShaderCounter.NodeVisitSlots)):P0}"),
            new("BVH box tests per ray", $"{Ratio(Get(ShaderCounter.BoxTests), rays):F1}"),
            new("Primitive tests per ray", $"{Ratio(Get(ShaderCounter.PrimitiveTests), rays):F1}"),
            new("Max traversal stack depth", $"{Get(ShaderCounter.MaxStackDepth):N0}"),
            new("Paths ending in the sky", $"{Ratio(Get(ShaderCounter.SkyEscapes), paths):P1}"),
            new("Paths absorbed", $"{Ratio(Get(ShaderCounter.Absorbed), paths):P1}"),
            new("Emitter hits per path", $"{Ratio(Get(ShaderCounter.EmitterHits), paths):F3}"),
            new("NaN samples", $"{Get(ShaderCounter.NanSamples):N0}"),
        ];
    }

    private string RaysByBounce(double rays)
    {
        double bounce1 = Get(ShaderCounter.RaysBounce1);
        double bounce2 = Get(ShaderCounter.RaysBounce2);
        double bounce3Plus = Get(ShaderCounter.RaysBounce3Plus);
        double primary = rays - bounce1 - bounce2 - bounce3Plus;

        return $"{Ratio(primary, rays):P0} / {Ratio(bounce1, rays):P0} / {Ratio(bounce2, rays):P0} / {Ratio(bounce3Plus, rays):P0}";
    }

    private double Get(ShaderCounter counter) => _values[(int)counter];

    private static double Ratio(double value, double total) => total > 0 ? value / total : 0;
}

using Pathetic.Profiling.Gpu;

namespace Pathetic.Profiling.Statistics;

public sealed class ShaderCounterStats
{
    private const double RaysPerMegaray = 1_000_000;

    private uint[] _values = [];
    private TimeSpan _gpuTime;

    internal void Update(uint[] values, TimeSpan gpuTime)
    {
        if (values.Length == 0)
            return;

        _values = values;
        _gpuTime = gpuTime;
    }

    public IReadOnlyList<(string Name, string Value)> Rows()
    {
        if (_values.Length == 0)
            return [];

        double paths = Get(ShaderCounter.Paths);
        double rays = Get(ShaderCounter.Rays);

        return
        [
            ("Paths per frame", $"{paths:N0}"),
            ("Rays per frame", $"{rays:N0}"),
            ("Rays per path", $"{Ratio(rays, paths):F2}"),
            ("Throughput", $"{Ratio(rays, _gpuTime.TotalSeconds) / RaysPerMegaray:F0} Mrays/s"),
            ("Rays by bounce (0 / 1 / 2 / 3+)", RaysByBounce(rays)),
            ("Rays missing the BVH root", $"{Ratio(Get(ShaderCounter.RootMisses), rays):P1}"),
            ("BVH nodes visited per ray", $"{Ratio(Get(ShaderCounter.NodeVisits), rays):F1}"),
            ("SIMD efficiency in traversal", $"{Ratio(Get(ShaderCounter.NodeVisits), Get(ShaderCounter.NodeVisitSlots)):P0}"),
            ("BVH box tests per ray", $"{Ratio(Get(ShaderCounter.BoxTests), rays):F1}"),
            ("Primitive tests per ray", $"{Ratio(Get(ShaderCounter.PrimitiveTests), rays):F1}"),
            ("Max traversal stack depth", $"{Get(ShaderCounter.MaxStackDepth):N0}"),
            ("Paths ending in the sky", $"{Ratio(Get(ShaderCounter.SkyEscapes), paths):P1}"),
            ("Paths absorbed", $"{Ratio(Get(ShaderCounter.Absorbed), paths):P1}"),
            ("Paths cut by max bounces", $"{Ratio(Get(ShaderCounter.MaxBounceExits), paths):P1}"),
            ("Emitter hits per path", $"{Ratio(Get(ShaderCounter.EmitterHits), paths):F3}"),
            ("NaN samples", $"{Get(ShaderCounter.NanSamples):N0}"),
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

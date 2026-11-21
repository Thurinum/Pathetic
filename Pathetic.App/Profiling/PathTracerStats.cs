using Pathetic.App.Generated;
using Pathetic.Profiling.Statistics;

namespace Pathetic.App.Profiling;

/// <summary>What the path tracer counted during the last frame. The rows are generated from the attributes.</summary>
public sealed partial class PathTracerStats : ShaderCounterStats
{
    private const double RaysPerMegaray = 1_000_000;

    [ShaderCounter("Paths per frame", Order = 1, Format = "N0")]
    private double Paths => Get(ShaderCounter.Paths);

    [ShaderCounter("Rays per frame", Order = 2, Format = "N0")]
    private double Rays => Get(ShaderCounter.Rays);

    [ShaderCounter("Rays per path", Order = 3, Format = "F2")]
    private double RaysPerPath => Ratio(Rays, Paths);

    [ShaderCounter("Throughput", Order = 4, Format = "F0", Suffix = " Mrays/s")]
    private double Throughput => Ratio(Rays, GpuTime.TotalSeconds) / RaysPerMegaray;

    [ShaderCounter("Rays by bounce (0 / 1 / 2 / 3+)", Order = 5)]
    private string RaysByBounce
    {
        get
        {
            double bounce1 = Get(ShaderCounter.RaysBounce1);
            double bounce2 = Get(ShaderCounter.RaysBounce2);
            double bounce3Plus = Get(ShaderCounter.RaysBounce3Plus);
            double primary = Rays - bounce1 - bounce2 - bounce3Plus;

            return $"{Ratio(primary, Rays):P0} / {Ratio(bounce1, Rays):P0} / {Ratio(bounce2, Rays):P0} / {Ratio(bounce3Plus, Rays):P0}";
        }
    }

    [ShaderCounter("Rays missing the BVH root", Order = 6, Format = "P1")]
    private double RootMisses => Ratio(Get(ShaderCounter.RootMisses), Rays);

    [ShaderCounter("BVH nodes visited per ray", Order = 7, Format = "F1")]
    private double NodeVisitsPerRay => Ratio(Get(ShaderCounter.NodeVisits), Rays);

    [ShaderCounter("SIMD efficiency in traversal", Order = 8, Format = "P0")]
    private double SimdEfficiency => Ratio(Get(ShaderCounter.NodeVisits), Get(ShaderCounter.NodeVisitSlots));

    [ShaderCounter("BVH box tests per ray", Order = 9, Format = "F1")]
    private double BoxTestsPerRay => Ratio(Get(ShaderCounter.BoxTests), Rays);

    [ShaderCounter("Primitive tests per ray", Order = 10, Format = "F1")]
    private double PrimitiveTestsPerRay => Ratio(Get(ShaderCounter.PrimitiveTests), Rays);

    [ShaderCounter("Max traversal stack depth", Order = 11, Format = "N0")]
    private double MaxStackDepth => Get(ShaderCounter.MaxStackDepth);

    [ShaderCounter("Paths ending in the sky", Order = 12, Format = "P1")]
    private double SkyEscapes => Ratio(Get(ShaderCounter.SkyEscapes), Paths);

    [ShaderCounter("Paths absorbed", Order = 13, Format = "P1")]
    private double Absorbed => Ratio(Get(ShaderCounter.Absorbed), Paths);

    [ShaderCounter("Emitter hits per path", Order = 14, Format = "F3")]
    private double EmitterHits => Ratio(Get(ShaderCounter.EmitterHits), Paths);

    [ShaderCounter("NaN samples", Order = 15, Format = "N0")]
    private double NanSamples => Get(ShaderCounter.NanSamples);
}

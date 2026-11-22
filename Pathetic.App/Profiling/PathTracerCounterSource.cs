using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pathetic.App.RenderPasses;
using Pathetic.Profiling.Gpu;
using Pathetic.Profiling.Statistics;

namespace Pathetic.App.Profiling;

/// <summary>Presents <see cref="PathTracerCounters"/> in the profiler.</summary>
public sealed class PathTracerCounterSource : IShaderCounterSource
{
    private const double RaysPerMegaray = 1_000_000;

    public string Pass => nameof(PathTracerPass);

    public uint SizeInBytes => (uint)Unsafe.SizeOf<PathTracerCounters>();

    public IReadOnlyList<ShaderCounterRow> Rows(ReadOnlySpan<byte> counters, TimeSpan passTime)
    {
        PathTracerCounters c = MemoryMarshal.Read<PathTracerCounters>(counters);
        double bounce0 = c.Rays - c.RaysBounce1 - c.RaysBounce2 - c.RaysBounce3Plus;

        return
        [
            new("Paths per frame", $"{c.Paths:N0}"),
            new("Rays per frame", $"{c.Rays:N0}"),
            new("Rays per path", $"{Ratio(c.Rays, c.Paths):F2}"),
            new("Throughput", $"{Ratio(c.Rays, passTime.TotalSeconds) / RaysPerMegaray:F0} Mrays/s"),
            new("Rays by bounce (0 / 1 / 2 / 3+)",
                $"{Ratio(bounce0, c.Rays):P0} / {Ratio(c.RaysBounce1, c.Rays):P0} / {Ratio(c.RaysBounce2, c.Rays):P0} / {Ratio(c.RaysBounce3Plus, c.Rays):P0}"),
            new("Rays missing the BVH root", $"{Ratio(c.RootMisses, c.Rays):P1}"),
            new("BVH nodes visited per ray", $"{Ratio(c.NodeVisits, c.Rays):F1}"),
            new("SIMD efficiency in traversal", $"{Ratio(c.NodeVisits, c.NodeVisitSlots):P0}"),
            new("BVH box tests per ray", $"{Ratio(c.BoxTests, c.Rays):F1}"),
            new("Primitive tests per ray", $"{Ratio(c.PrimitiveTests, c.Rays):F1}"),
            new("Max traversal stack depth", $"{c.MaxStackDepth:N0}"),
            new("Paths ending in the sky", $"{Ratio(c.SkyEscapes, c.Paths):P1}"),
            new("Paths absorbed", $"{Ratio(c.Absorbed, c.Paths):P1}"),
            new("Emitter hits per path", $"{Ratio(c.EmitterHits, c.Paths):F3}"),
            new("NaN samples", $"{c.NanSamples:N0}"),
        ];
    }

    private static double Ratio(double value, double total) => total > 0 ? value / total : 0;
}

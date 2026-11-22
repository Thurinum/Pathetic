using Pathetic.Profiling.Statistics;

namespace Pathetic.Profiling.Gpu;

/// <summary>
/// The counters a shader writes: how big their GPU struct is and how to present it.
/// The profiler stays generic; each renderer feature describes its own counters.
/// </summary>
public interface IShaderCounterSource
{
    /// <summary>The render pass that writes the counters. Rates are measured against its GPU time.</summary>
    string Pass { get; }

    uint SizeInBytes { get; }

    IReadOnlyList<ShaderCounterRow> Rows(ReadOnlySpan<byte> counters, TimeSpan passTime);
}

using System.Runtime.InteropServices;

namespace Pathetic.App.Profiling;

/// <summary>What the path tracer counts, per frame. Same fields, in the same order, as PathTracerCounters in PathTracer.slang.</summary>
[StructLayout(LayoutKind.Sequential)]
public record struct PathTracerCounters
{
    public uint Paths;
    public uint Rays;
    public uint RaysBounce1;
    public uint RaysBounce2;
    public uint RaysBounce3Plus;
    public uint RootMisses;
    public uint BoxTests;
    public uint NodeVisits;
    public uint NodeVisitSlots;
    public uint PrimitiveTests;
    public uint MaxStackDepth;
    public uint SkyEscapes;
    public uint Absorbed;
    public uint EmitterHits;
    public uint NanSamples;
}

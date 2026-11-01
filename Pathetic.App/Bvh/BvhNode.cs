using System.Numerics;
using System.Runtime.InteropServices;

namespace Pathetic.App.Bvh;

public readonly record struct BvhPrimitiveRef(
    PrimitiveType Type,
    uint Index,
    AxisAlignedBoundingBox Bounds);

public sealed class BvhNode
{
    public required AxisAlignedBoundingBox Bounds { get; init; }
    public BvhNode? Left { get; init; }
    public BvhNode? Right { get; init; }
    public List<BvhPrimitiveRef>? Primitives { get; init; }
}

// Matches the std430 layout Slang uses for a StructuredBuffer element. A float3 is
// 16-byte aligned but only 12 bytes wide, so Max starts at 16 and Left at 28.
[StructLayout(LayoutKind.Explicit, Size = 48)]
public record struct GpuBvhNode
{
    [FieldOffset(00)] public Vector3 Min;
    [FieldOffset(16)] public Vector3 Max;
    [FieldOffset(28)] public uint Left;
    [FieldOffset(32)] public uint Right;
    [FieldOffset(36)] public uint FirstPrimitive;
    [FieldOffset(40)] public uint PrimitiveCount;
}

[StructLayout(LayoutKind.Explicit, Size = 8)]
public record struct GpuBvhPrimitiveRef
{
    [FieldOffset(0)] public uint Type;
    [FieldOffset(4)] public uint Index;
}

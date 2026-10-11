using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracerCore.Bvh;

#region CPU
public readonly record struct BvhPrimitiveRef(
    uint Type,
    uint Index,
    AxisAlignedBoundingBox Bounds);
    
public sealed class BvhNode
{
    public required AxisAlignedBoundingBox Bounds;
    public BvhNode? Left;
    public BvhNode? Right;
    public List<BvhPrimitiveRef>? Primitives;
}
#endregion

#region GPU
[StructLayout(LayoutKind.Explicit, Size = 40)]
public struct GpuBvhNode
{
    [FieldOffset(0)]  public Vector3 Min;
    [FieldOffset(12)] public Vector3 Max;
    [FieldOffset(24)] public uint Left;
    [FieldOffset(28)] public uint Right;
    [FieldOffset(32)] public uint FirstPrimitive;
    [FieldOffset(36)] public uint PrimitiveCount;
}

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct GpuBvhPrimitiveRef
{
    [FieldOffset(0)] public uint Type;
    [FieldOffset(4)] public uint Index;
}
#endregion
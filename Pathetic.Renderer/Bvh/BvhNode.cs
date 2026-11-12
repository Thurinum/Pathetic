using System.Numerics;
using System.Runtime.InteropServices;

namespace Pathetic.Renderer.Bvh;

#region CPU
public readonly record struct BvhPrimitiveRef(
    uint Type,
    uint Index,
    AxisAlignedBoundingBox Bounds);

public sealed class BvhNode
{
    public required AxisAlignedBoundingBox Bounds { get; init; }
    public BvhNode? Left { get; init; }
    public BvhNode? Right { get; init; }
    public List<BvhPrimitiveRef>? Primitives { get; init; }
}
#endregion

#region GPU
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct UInt4
{
    public uint X;
    public uint Y;
    public uint Z;
    public uint W;

    public readonly uint GetLane(int lane) => lane switch
    {
        0 => X,
        1 => Y,
        2 => Z,
        3 => W,
        _ => throw new ArgumentOutOfRangeException(nameof(lane))
    };

    public void SetLane(int lane, uint value)
    {
        switch (lane)
        {
            case 0: X = value; break;
            case 1: Y = value; break;
            case 2: Z = value; break;
            case 3: W = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(lane));
        }
    }
}

[StructLayout(LayoutKind.Explicit, Size = 128)]
public struct GpuBvh8Node
{
    [FieldOffset(0)] public Vector3 Min;
    [FieldOffset(12)] public Vector3 Max;
    [FieldOffset(24)] public UInt4 ChildMin0;
    [FieldOffset(40)] public UInt4 ChildMin1;
    [FieldOffset(56)] public UInt4 ChildMax0;
    [FieldOffset(72)] public UInt4 ChildMax1;
    [FieldOffset(88)] public UInt4 ChildRef0;
    [FieldOffset(104)] public UInt4 ChildRef1;
    [FieldOffset(120)] public uint ChildCount;
    [FieldOffset(124)] public uint Padding;

    public void SetChild(int slot, uint packedMin, uint packedMax, uint reference)
    {
        switch (slot / 4)
        {
            case 0:
                ChildMin0.SetLane(slot % 4, packedMin);
                ChildMax0.SetLane(slot % 4, packedMax);
                ChildRef0.SetLane(slot % 4, reference);
                break;
            case 1:
                ChildMin1.SetLane(slot % 4, packedMin);
                ChildMax1.SetLane(slot % 4, packedMax);
                ChildRef1.SetLane(slot % 4, reference);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }
}

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct GpuBvhPrimitiveRef
{
    [FieldOffset(0)] public uint Type;
    [FieldOffset(4)] public uint Index;
}
#endregion

using System.Numerics;
using System.Runtime.InteropServices;

namespace Pathetic.Renderer.Bvh;

#region CPU

public readonly record struct BvhPrimitiveRef(
    uint Index,
    AxisAlignedBoundingBox Bounds);

public sealed class BvhNode
{
    public required AxisAlignedBoundingBox Bounds { get; init; }
    public BvhNode? Left { get; init; }
    public BvhNode? Right { get; init; }
    public List<BvhPrimitiveRef>? Primitives { get; init; }

    public uint Flatten(List<GpuBvhNode> nodes, List<GpuBvhPrimitiveRef> primitiveRefs)
    {
        return FlattenInternal(this, nodes, primitiveRefs);
    }

    private uint FlattenInternal(
        BvhNode node,
        List<GpuBvhNode> nodes,
        List<GpuBvhPrimitiveRef> primitiveRefs)
    {
        uint nodeIndex = (uint)nodes.Count;
        nodes.Add(default);

        GpuBvhNode gpuNode = new()
        {
            Min = node.Bounds.Min,
            Max = node.Bounds.Max,
            Left = uint.MaxValue,
            Right = uint.MaxValue
        };

        if (node.Primitives is not null)
        {
            gpuNode.FirstPrimitive = (uint)primitiveRefs.Count;
            gpuNode.PrimitiveCount = (uint)node.Primitives.Count;

            primitiveRefs.AddRange(node.Primitives.Select(primitive => new GpuBvhPrimitiveRef
            {
                Index = primitive.Index
            }));
        }
        else
        {
            if (node.Left is null || node.Right is null)
                throw new InvalidOperationException("Internal BVH nodes must have two children.");

            gpuNode.Left = FlattenInternal(node.Left, nodes, primitiveRefs);
            gpuNode.Right = FlattenInternal(node.Right, nodes, primitiveRefs);
            gpuNode.PrimitiveCount = 0;
        }

        nodes[(int)nodeIndex] = gpuNode;
        return nodeIndex;
    }
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

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct GpuBvhPrimitiveRef
{
    [FieldOffset(0)] public uint Index;
}
#endregion

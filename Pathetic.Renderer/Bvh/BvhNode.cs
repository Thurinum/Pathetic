using System.Numerics;
using SlangGen = Pathetic.Generated.Slang;

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

    public uint Flatten(List<SlangGen.Bvh.BvhNode> nodes, List<SlangGen.Bvh.BvhPrimitiveRef> primitiveRefs)
    {
        return FlattenInternal(this, nodes, primitiveRefs);
    }

    private uint FlattenInternal(
        BvhNode node,
        List<SlangGen.Bvh.BvhNode> nodes,
        List<SlangGen.Bvh.BvhPrimitiveRef> primitiveRefs)
    {
        uint nodeIndex = (uint)nodes.Count;
        nodes.Add(default);

        SlangGen.Bvh.BvhNode gpuNode = new()
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

            primitiveRefs.AddRange(node.Primitives.Select(primitive => new SlangGen.Bvh.BvhPrimitiveRef
            {
                Type = (int)primitive.Type,
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

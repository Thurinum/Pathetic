using System.Runtime.InteropServices;
using Pathetic.Renderer.Bvh;

namespace Pathetic.App.Bvh;

// Everything here only exists to be shown by the profiler.
public sealed partial class SceneBvh
{
    private const int RootIndex = 0;
    private const int QualityDecimals = 2;
    private const float TraversalCost = 1f;
    private const float IntersectionCost = 1f;

    [ProfileTag("bvh.nodes")]
    internal int NodeCount => _nodes.Length;

    [ProfileTag("bvh.node_bytes")]
    internal int NodeBytes => MemoryMarshal.AsBytes(_nodes.AsSpan()).Length;

    [ProfileTag("bvh.primitive_refs")]
    internal int PrimitiveRefCount => _primitiveRefs.Length;

    [ProfileTag("bvh.depth")]
    internal int Depth => _nodes.Length == 0 ? 0 : DepthOf(RootIndex);

    [ProfileTag("bvh.leaves")]
    internal int LeafCount => _nodes.Count(IsLeaf);

    [ProfileTag("bvh.avg_leaf_size")]
    internal float AverageLeafSize => LeafCount == 0 ? 0 : MathF.Round((float)_primitiveRefs.Length / LeafCount, QualityDecimals);

    [ProfileTag("bvh.sah_cost")]
    internal float SahCost => _nodes.Length == 0 ? 0 : MathF.Round(_nodes.Sum(NodeCost) / SurfaceArea(_nodes[RootIndex]), QualityDecimals);

    private static bool IsLeaf(GPUTypes.BvhNode node) => node.PrimitiveCount > 0;

    private static float SurfaceArea(GPUTypes.BvhNode node) => new AxisAlignedBoundingBox(node.Min, node.Max).SurfaceArea;

    private static float NodeCost(GPUTypes.BvhNode node) =>
        SurfaceArea(node) * (IsLeaf(node) ? IntersectionCost * node.PrimitiveCount : TraversalCost);

    private int DepthOf(int nodeIndex)
    {
        GPUTypes.BvhNode node = _nodes[nodeIndex];
        return IsLeaf(node) ? 1 : 1 + Math.Max(DepthOf((int)node.Left), DepthOf((int)node.Right));
    }
}

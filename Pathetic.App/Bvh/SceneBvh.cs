using System.Numerics;
using System.Runtime.InteropServices;
using Pathetic.App.Components;
using Pathetic.Renderer.Bvh;
using Pathetic.Renderer.Buffers;

namespace Pathetic.App.Bvh;

public sealed class SceneBvh(
    StructuredBuffer<SpherePrimitive> spheres,
    StructuredBuffer<PlanePrimitive> planes)
{
    private const uint PlaneType = 0;
    private const uint SphereType = 1;
    private const int RootIndex = 0;
    private const int QualityDecimals = 2;
    private const float TraversalCost = 1f;
    private const float IntersectionCost = 1f;

    private GpuBvhNode[] _nodes = [];
    private GpuBvhPrimitiveRef[] _primitiveRefs = [];
    private uint _sphereRevision;
    private uint _planeRevision;
    private bool _built;

    public uint Version { get; private set; }
    public GpuBvhNode[] Nodes => _nodes;
    public GpuBvhPrimitiveRef[] PrimitiveRefs => _primitiveRefs;

    [ProfileTag("bvh.nodes")]
    public int NodeCount => _nodes.Length;

    [ProfileTag("bvh.node_bytes")]
    public int NodeBytes => MemoryMarshal.AsBytes(_nodes.AsSpan()).Length;

    [ProfileTag("bvh.primitive_refs")]
    public int PrimitiveRefCount => _primitiveRefs.Length;

    [ProfileTag("bvh.depth")]
    public int Depth => _nodes.Length == 0 ? 0 : DepthOf(RootIndex);

    [ProfileTag("bvh.leaves")]
    public int LeafCount => _nodes.Count(IsLeaf);

    [ProfileTag("bvh.avg_leaf_size")]
    public float AverageLeafSize => LeafCount == 0 ? 0 : MathF.Round((float)_primitiveRefs.Length / LeafCount, QualityDecimals);

    [ProfileTag("bvh.sah_cost")]
    public float SahCost => _nodes.Length == 0 ? 0 : MathF.Round(_nodes.Sum(NodeCost) / SurfaceArea(_nodes[RootIndex]), QualityDecimals);

    public bool IsDirty =>
        !_built
        || _sphereRevision != spheres.Revision
        || _planeRevision != planes.Revision;

    [Profile]
    public void Rebuild()
    {
        List<BvhPrimitiveRef> references = [];
        AppendSpheres(references);
        AppendPlanes(references);

        List<GpuBvhNode> nodes = [];
        List<GpuBvhPrimitiveRef> primitiveRefs = [];

        if (references.Count > 0)
        {
            BvhNode root = BvhBuilder.Build(references);
            root.Flatten(nodes, primitiveRefs);
        }

        _nodes = nodes.ToArray();
        _primitiveRefs = primitiveRefs.ToArray();
        _sphereRevision = spheres.Revision;
        _planeRevision = planes.Revision;
        _built = true;
        Version++;
    }

    private static bool IsLeaf(GpuBvhNode node) => node.PrimitiveCount > 0;

    private static float SurfaceArea(GpuBvhNode node) => new AxisAlignedBoundingBox(node.Min, node.Max).SurfaceArea;

    private static float NodeCost(GpuBvhNode node) =>
        SurfaceArea(node) * (IsLeaf(node) ? IntersectionCost * node.PrimitiveCount : TraversalCost);

    private int DepthOf(int nodeIndex)
    {
        GpuBvhNode node = _nodes[nodeIndex];
        return IsLeaf(node) ? 1 : 1 + Math.Max(DepthOf((int)node.Left), DepthOf((int)node.Right));
    }

    private void AppendSpheres(List<BvhPrimitiveRef> references)
    {
        ReadOnlySpan<SpherePrimitive> source = spheres.Values;
        for (uint i = 0; i < source.Length; i++)
        {
            SpherePrimitive sphere = source[(int)i];
            Vector3 radius = new(sphere.Radius);
            references.Add(new BvhPrimitiveRef(
                SphereType,
                i,
                new AxisAlignedBoundingBox(sphere.Center - radius, sphere.Center + radius)));
        }
    }

    private void AppendPlanes(List<BvhPrimitiveRef> references)
    {
        ReadOnlySpan<PlanePrimitive> source = planes.Values;
        for (uint i = 0; i < source.Length; i++)
        {
            PlanePrimitive plane = source[(int)i];
            Vector3 p0 = plane.Point - plane.ExtentX - plane.ExtentY;
            Vector3 p1 = plane.Point + plane.ExtentX - plane.ExtentY;
            Vector3 p2 = plane.Point - plane.ExtentX + plane.ExtentY;
            Vector3 p3 = plane.Point + plane.ExtentX + plane.ExtentY;

            AxisAlignedBoundingBox bounds = AxisAlignedBoundingBox.Empty
                .Include(p0).Include(p1).Include(p2).Include(p3);

            references.Add(new BvhPrimitiveRef(PlaneType, i, bounds));
        }
    }

}

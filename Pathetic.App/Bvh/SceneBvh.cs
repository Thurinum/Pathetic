using System.Numerics;
using Pathetic.App.Components;
using Pathetic.Renderer.Buffers;

namespace Pathetic.App.Bvh;

public sealed record BvhGpuData(GpuBvhNode[] Nodes, GpuBvhPrimitiveRef[] PrimitiveRefs);

/// <summary>
/// Builds and uploads the acceleration structure for every analytic primitive and mesh
/// triangle in the scene. Rebuilds only when one of the source buffers changed.
/// </summary>
public sealed class SceneBvh(
    StructuredBuffer<SpherePrimitive> spheres,
    StructuredBuffer<PlanePrimitive> planes,
    StructuredBuffer<TrianglePrimitive> triangles,
    StructuredBuffer<GpuBvhNode> nodes,
    StructuredBuffer<GpuBvhPrimitiveRef> primitiveRefs)
{
    private BvhNode? _root;
    private uint _sphereRevision = uint.MaxValue;
    private uint _planeRevision = uint.MaxValue;
    private uint _triangleRevision = uint.MaxValue;

    public bool IsDirty =>
        _sphereRevision != spheres.Revision
        || _planeRevision != planes.Revision
        || _triangleRevision != triangles.Revision;

    public int NodeCount => nodes.Count;
    public int PrimitiveRefCount => primitiveRefs.Count;

    public void Update()
    {
        if (!IsDirty)
            return;

        List<BvhPrimitiveRef> references = [];

        for (int i = 0; i < spheres.Count; i++)
        {
            SpherePrimitive sphere = spheres.Values[i];
            Vector3 radius = new(sphere.Radius);

            references.Add(new BvhPrimitiveRef(
                PrimitiveType.Sphere,
                (uint)i,
                new AxisAlignedBoundingBox(sphere.Center - radius, sphere.Center + radius)));
        }

        for (int i = 0; i < planes.Count; i++)
        {
            PlanePrimitive plane = planes.Values[i];
            Vector3 p0 = plane.Point - plane.ExtentX - plane.ExtentY;
            Vector3 p1 = plane.Point + plane.ExtentX - plane.ExtentY;
            Vector3 p2 = plane.Point - plane.ExtentX + plane.ExtentY;
            Vector3 p3 = plane.Point + plane.ExtentX + plane.ExtentY;

            AxisAlignedBoundingBox bounds = AxisAlignedBoundingBox.Empty
                .Include(p0)
                .Include(p1)
                .Include(p2)
                .Include(p3);

            references.Add(new BvhPrimitiveRef(PrimitiveType.Plane, (uint)i, bounds));
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            TrianglePrimitive triangle = triangles.Values[i];
            AxisAlignedBoundingBox bounds = AxisAlignedBoundingBox.Empty
                .Include(triangle.V0)
                .Include(triangle.V1)
                .Include(triangle.V2);

            references.Add(new BvhPrimitiveRef(PrimitiveType.Triangle, (uint)i, bounds));
        }

        _root = references.Count == 0 ? null : new BvhBuilder().Build(references);

        BvhGpuData data = Flatten();
        nodes.SetAll(data.Nodes);
        primitiveRefs.SetAll(data.PrimitiveRefs);

        _sphereRevision = spheres.Revision;
        _planeRevision = planes.Revision;
        _triangleRevision = triangles.Revision;
    }

    private BvhGpuData Flatten()
    {
        if (_root is null)
            return new BvhGpuData([], []);

        List<GpuBvhNode> flattenedNodes = [];
        List<GpuBvhPrimitiveRef> flattenedPrimitiveRefs = [];

        FlattenNode(_root, flattenedNodes, flattenedPrimitiveRefs);

        return new BvhGpuData(flattenedNodes.ToArray(), flattenedPrimitiveRefs.ToArray());
    }

    private static uint FlattenNode(
        BvhNode node,
        List<GpuBvhNode> flattenedNodes,
        List<GpuBvhPrimitiveRef> flattenedPrimitiveRefs)
    {
        uint nodeIndex = (uint)flattenedNodes.Count;
        flattenedNodes.Add(default);

        if (node.Primitives is not null)
        {
            GpuBvhNode leaf = new()
            {
                Min = node.Bounds.Min,
                Max = node.Bounds.Max,
                Left = uint.MaxValue,
                Right = uint.MaxValue,
                FirstPrimitive = (uint)flattenedPrimitiveRefs.Count,
                PrimitiveCount = (uint)node.Primitives.Count,
            };

            foreach (BvhPrimitiveRef primitive in node.Primitives)
            {
                flattenedPrimitiveRefs.Add(new GpuBvhPrimitiveRef
                {
                    Type = (uint)primitive.Type,
                    Index = primitive.Index,
                });
            }

            flattenedNodes[(int)nodeIndex] = leaf;
            return nodeIndex;
        }

        if (node.Left is null || node.Right is null)
            throw new InvalidOperationException("Internal BVH nodes must have two children.");

        uint left = FlattenNode(node.Left, flattenedNodes, flattenedPrimitiveRefs);
        uint right = FlattenNode(node.Right, flattenedNodes, flattenedPrimitiveRefs);

        flattenedNodes[(int)nodeIndex] = new GpuBvhNode
        {
            Min = node.Bounds.Min,
            Max = node.Bounds.Max,
            Left = left,
            Right = right,
            FirstPrimitive = 0,
            PrimitiveCount = 0,
        };

        return nodeIndex;
    }
}

using System.Numerics;
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

    private GpuBvh8Node[] _nodes = [];
    private GpuBvhPrimitiveRef[] _primitiveRefs = [];
    private uint _sphereRevision;
    private uint _planeRevision;
    private bool _built;

    public uint Version { get; private set; }
    public GpuBvh8Node[] Nodes => _nodes;
    public GpuBvhPrimitiveRef[] PrimitiveRefs => _primitiveRefs;

    public bool IsDirty =>
        !_built
        || _sphereRevision != spheres.Revision
        || _planeRevision != planes.Revision;

    public void Rebuild()
    {
        List<BvhPrimitiveRef> references = [];
        AppendSpheres(references);
        AppendPlanes(references);

        List<GpuBvh8Node> nodes = [];
        List<GpuBvhPrimitiveRef> primitiveRefs = [];

        if (references.Count > 0)
        {
            BvhNode root = new BvhBuilder().Build(references);
            FlattenWide(root, nodes, primitiveRefs);
        }

        _nodes = nodes.ToArray();
        _primitiveRefs = primitiveRefs.ToArray();
        _sphereRevision = spheres.Revision;
        _planeRevision = planes.Revision;
        _built = true;
        Version++;
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

    private static uint FlattenWide(
        BvhNode node,
        List<GpuBvh8Node> nodes,
        List<GpuBvhPrimitiveRef> primitiveRefs)
    {
        uint nodeIndex = (uint)nodes.Count;
        nodes.Add(default);

        List<BvhNode> children = GetWideChildren(node);
        GpuBvh8Node gpuNode = new()
        {
            Min = node.Bounds.Min,
            Max = node.Bounds.Max,
            ChildCount = (uint)children.Count
        };

        for (int slot = 0; slot < children.Count; slot++)
        {
            BvhNode child = children[slot];
            uint reference;
            if (child.Primitives is not null)
            {
                if (child.Primitives.Count is < 1 or > 3)
                    throw new InvalidOperationException("Compressed BVH leaves must contain between one and three primitives.");

                uint firstPrimitive = (uint)primitiveRefs.Count;
                if (firstPrimitive + (uint)child.Primitives.Count > (1u << 29))
                    throw new InvalidOperationException("Compressed BVH supports fewer than 2^29 primitive references.");

                reference = ((uint)(child.Primitives.Count - 1) << 29) | firstPrimitive;
                foreach (BvhPrimitiveRef primitive in child.Primitives)
                {
                    primitiveRefs.Add(new GpuBvhPrimitiveRef
                    {
                        Type = primitive.Type,
                        Index = primitive.Index
                    });
                }
            }
            else
            {
                uint childNodeIndex = FlattenWide(child, nodes, primitiveRefs);
                if (childNodeIndex >= 0x80000000u)
                    throw new InvalidOperationException("Compressed BVH supports fewer than 2^31 nodes.");
                reference = childNodeIndex | 0x80000000u;
            }

            (uint packedMin, uint packedMax) = QuantizeBounds(node.Bounds, child.Bounds);
            gpuNode.SetChild(slot, packedMin, packedMax, reference);
        }

        nodes[(int)nodeIndex] = gpuNode;
        return nodeIndex;
    }

    private static List<BvhNode> GetWideChildren(BvhNode node)
    {
        if (node.Primitives is not null)
            return [node];

        if (node.Left is null || node.Right is null)
            throw new InvalidOperationException("Internal BVH nodes must have two children.");

        List<BvhNode> children = [node.Left, node.Right];
        while (children.Count < 8)
        {
            int expandIndex = -1;
            float largestArea = float.NegativeInfinity;
            for (int i = 0; i < children.Count; i++)
            {
                BvhNode candidate = children[i];
                if (candidate.Primitives is not null)
                    continue;

                float area = candidate.Bounds.SurfaceArea;
                if (area > largestArea)
                {
                    largestArea = area;
                    expandIndex = i;
                }
            }

            if (expandIndex < 0)
                break;

            BvhNode expanded = children[expandIndex];
            if (expanded.Left is null || expanded.Right is null)
                throw new InvalidOperationException("Internal BVH nodes must have two children.");

            children[expandIndex] = expanded.Left;
            children.Insert(expandIndex + 1, expanded.Right);
        }

        return children;
    }

    private static (uint Min, uint Max) QuantizeBounds(
        AxisAlignedBoundingBox parent,
        AxisAlignedBoundingBox child)
    {
        Vector3 extent = parent.Max - parent.Min;
        uint min = PackQuantized(child.Min, parent.Min, extent, lower: true);
        uint max = PackQuantized(child.Max, parent.Min, extent, lower: false);
        return (min, max);
    }

    private static uint PackQuantized(Vector3 point, Vector3 origin, Vector3 extent, bool lower)
    {
        uint x = Quantize(point.X, origin.X, extent.X, lower);
        uint y = Quantize(point.Y, origin.Y, extent.Y, lower);
        uint z = Quantize(point.Z, origin.Z, extent.Z, lower);
        return x | (y << 8) | (z << 16);
    }

    private static uint Quantize(float value, float origin, float extent, bool lower)
    {
        if (!(extent > 0.0f) || !float.IsFinite(extent))
            return lower ? 0u : 255u;

        float normalized = Math.Clamp((value - origin) / extent, 0.0f, 1.0f) * 255.0f;
        int quantized = lower
            ? (int)MathF.Floor(normalized) - 1
            : (int)MathF.Ceiling(normalized) + 1;
        return (uint)Math.Clamp(quantized, 0, 255);
    }
}

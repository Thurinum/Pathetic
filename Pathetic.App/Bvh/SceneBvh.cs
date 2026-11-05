using System.Numerics;
using Pathetic.App.Components;
using Pathetic.Renderer.Bvh;
using Pathetic.Renderer.Buffers;

namespace Pathetic.App.Bvh;

public sealed class SceneBvh(SparseSet<SpherePrimitive> spheres)
{
    private GpuBvhNode[] _nodes = [];
    private GpuBvhPrimitiveRef[] _primitiveRefs = [];
    private uint _sphereRevision;
    private bool _built;

    public uint Version { get; private set; }
    public GpuBvhNode[] Nodes => _nodes;
    public GpuBvhPrimitiveRef[] PrimitiveRefs => _primitiveRefs;

    public bool IsDirty => !_built || _sphereRevision != spheres.Buffer.Revision;

    public bool EnsureBuilt()
    {
        if (!IsDirty)
            return false;

        Build();
        _sphereRevision = spheres.Buffer.Revision;
        _built = true;
        Version++;
        return true;
    }

    private void Build()
    {
        ReadOnlySpan<SpherePrimitive> source = spheres.Buffer.Values;
        if (source.Length == 0)
        {
            _nodes = [];
            _primitiveRefs = [];
            return;
        }

        List<BvhPrimitiveRef> references = new(source.Length);
        for (uint i = 0; i < source.Length; i++)
        {
            SpherePrimitive sphere = source[(int)i];
            Vector3 radius = new(sphere.Radius);
            references.Add(new BvhPrimitiveRef(
                BvhPrimitiveType.Sphere,
                i,
                new AxisAlignedBoundingBox(sphere.Center - radius, sphere.Center + radius)));
        }

        BvhNode root = new BvhBuilder().Build(references);

        List<GpuBvhNode> nodes = [];
        List<GpuBvhPrimitiveRef> primitiveRefs = [];
        Flatten(root, nodes, primitiveRefs);

        _nodes = nodes.ToArray();
        _primitiveRefs = primitiveRefs.ToArray();
    }

    private static uint Flatten(
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
                Type = (uint)primitive.Type,
                Index = primitive.Index
            }));
        }
        else
        {
            if (node.Left is null || node.Right is null)
                throw new InvalidOperationException("Internal BVH nodes must have two children.");

            gpuNode.Left = Flatten(node.Left, nodes, primitiveRefs);
            gpuNode.Right = Flatten(node.Right, nodes, primitiveRefs);
            gpuNode.PrimitiveCount = 0;
        }

        nodes[(int)nodeIndex] = gpuNode;
        return nodeIndex;
    }
}

using Pathetic.App.Components;
using Pathetic.Renderer.Bvh;
using Pathetic.Renderer.Buffers;

namespace Pathetic.App.Bvh;

public sealed class SceneBvh(StructuredBuffer<Primitive> primitives)
{
    private GpuBvhNode[] _nodes = [];
    private GpuBvhPrimitiveRef[] _primitiveRefs = [];
    private uint _primitiveRevision;
    private bool _built;

    public uint Version { get; private set; }
    public GpuBvhNode[] Nodes => _nodes;
    public GpuBvhPrimitiveRef[] PrimitiveRefs => _primitiveRefs;

    public bool IsDirty => !_built || _primitiveRevision != primitives.Revision;

    public void Rebuild()
    {
        List<BvhPrimitiveRef> references = [];
        AppendPrimitives(references);

        List<GpuBvhNode> nodes = [];
        List<GpuBvhPrimitiveRef> primitiveRefs = [];

        if (references.Count > 0)
        {
            BvhNode root = new BvhBuilder().Build(references);
            Flatten(root, nodes, primitiveRefs);
        }

        _nodes = nodes.ToArray();
        _primitiveRefs = primitiveRefs.ToArray();
        _primitiveRevision = primitives.Revision;
        _built = true;
        Version++;
    }

    private void AppendPrimitives(List<BvhPrimitiveRef> references)
    {
        ReadOnlySpan<Primitive> source = primitives.Values;
        for (uint i = 0; i < source.Length; i++)
        {
            Primitive primitive = source[(int)i];
            if (primitive.Type == PrimitiveType.Circle)
                continue;

            references.Add(new BvhPrimitiveRef(i, new AxisAlignedBoundingBox(primitive.Min, primitive.Max)));
        }
    }

    private uint Flatten(
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

            primitiveRefs.AddRange(node.Primitives
                .OrderBy(primitive => primitives.Values[(int)primitive.Index].Type)
                .Select(primitive => new GpuBvhPrimitiveRef
                {
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

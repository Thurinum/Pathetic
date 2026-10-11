namespace PathTracerCore.Bvh;

using PathTracerCore.Renderer.Primitives;

public sealed record BvhGpuData(
    GpuBvhNode[] Nodes,
    GpuBvhPrimitiveRef[] PrimitiveRefs);

public sealed class SceneBvh(PrimitiveRegistry primitives)
{
    public BvhNode? Root { get; private set; }
    
    public BvhGpuData Flatten()
    {
        if (Root is null)
            return new BvhGpuData([], []);

        List<GpuBvhNode> nodes = [];
        List<GpuBvhPrimitiveRef> primitiveRefs = [];

        FlattenNode(Root, nodes, primitiveRefs);

        return new BvhGpuData(nodes.ToArray(), primitiveRefs.ToArray());
    }

    private static uint FlattenNode(
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
                    Type = primitive.Type,
                    Index = primitive.Index
                }));
        }
        else
        {
            if (node.Left is null || node.Right is null)
                throw new InvalidOperationException("Internal BVH nodes must have two children.");

            gpuNode.Left = FlattenNode(node.Left, nodes, primitiveRefs);
            gpuNode.Right = FlattenNode(node.Right, nodes, primitiveRefs);
            gpuNode.PrimitiveCount = 0;
        }

        nodes[(int)nodeIndex] = gpuNode;
        return nodeIndex;
    }
    
    private readonly Dictionary<IPrimitiveBuffer, uint> _sourceRevisions = [];

    public bool IsDirty =>
        primitives.All.Any(buffer =>
            !_sourceRevisions.TryGetValue(buffer, out var revision)
            || revision != buffer.Revision);

    public void Rebuild()
    {
        List<BvhPrimitiveRef> references = [];

        foreach (var buffer in primitives.All)
        {
            buffer.AppendBvhPrimitives(references);
            _sourceRevisions[buffer] = buffer.Revision;
        }

        Root = references.Count == 0
            ? null
            : new BvhBuilder().Build(references);
    }
}
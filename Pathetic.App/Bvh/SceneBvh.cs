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
            BvhNode root = BvhBuilder.Build(references);
            root.Flatten(nodes, primitiveRefs);
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
}

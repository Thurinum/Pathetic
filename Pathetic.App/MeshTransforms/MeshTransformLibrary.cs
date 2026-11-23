using Pathetic.Renderer.Buffers;

namespace Pathetic.App.MeshTransforms;

public sealed class MeshTransformLibrary(StructuredBuffer<Gpu.MeshTransform> transforms)
{
    public const uint None = uint.MaxValue;

    private readonly Stack<uint> _free = [];

    public uint Add(in Gpu.MeshTransform transform)
    {
        if (_free.Count > 0)
        {
            uint index = _free.Pop();
            transforms.UpdateAt((int)index, transform);
            return index;
        }

        return (uint)transforms.Add(transform);
    }

    public void Update(uint index, in Gpu.MeshTransform transform) => transforms.UpdateAt((int)index, transform);

    public void Remove(uint index) => _free.Push(index);
}

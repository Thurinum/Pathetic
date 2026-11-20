namespace Pathetic.Renderer.Buffers;

public sealed class GpuPool<T>(StructuredBuffer<T> buffer) where T : unmanaged, IEquatable<T>
{
    public const uint InvalidIndex = uint.MaxValue;

    private readonly Stack<uint> _free = new();

    public uint Add(in T value)
    {
        if (_free.Count == 0)
            return (uint)buffer.Add(value);

        uint index = _free.Pop();
        buffer.UpdateAt((int)index, value);
        return index;
    }

    public void Update(uint index, in T value)
    {
        buffer.UpdateAt((int)index, value);
    }

    public void Remove(uint index)
    {
        _free.Push(index);
    }
}

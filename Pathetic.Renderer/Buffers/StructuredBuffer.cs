using System.Runtime.CompilerServices;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Buffers;

public sealed class StructuredBuffer<T>(
    string name,
    bool resizable,
    bool deltaUpload,
    uint capacity = 256) : IStructuredBufferSpec where T : unmanaged, IEquatable<T>
{
    public string Name { get; } = name;
    public int Count { get; private set; }
    public uint Stride => (uint)Unsafe.SizeOf<T>();
    public uint ElementCount => (uint)Count;
    public uint InitialCapacity => capacity;
    public bool IsResizable => resizable;
    public bool DeltaUpload => deltaUpload;
    public IReadOnlyList<IResourceSpec> Resources => [this];
    
    private T[] _values = new T[capacity];
    private ReadOnlySpan<T> Data => _values.AsSpan(0, Count);
    private int Capacity => _values.Length;
    private bool[]? _dirtyBits = deltaUpload ? new bool[capacity] : null;
    
    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        if (!deltaUpload)
        {
            if (Count > 0)
                ctx.Cmd.UpdateBuffer(buffer, 0, Data);

            return;
        }

        if (resized)
            MarkAllDirty();

        for (int i = 0; i < Count; i++)
        {
            if (IsDirty(i))
                ctx.Cmd.UpdateBuffer(buffer, (uint)i * Stride, Data[i]);
        }

        ClearDirty();
    }

    public int Add(in T value)
    {
        EnsureCapacity(Count + 1);
        int index = Count++;
        _values[index] = value;
        MarkDirty(index);
        return index;
    }

    public void UpdateAt(int index, in T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        if (_values[index].Equals(value))
            return;

        _values[index] = value;
        MarkDirty(index);
    }
    
    internal void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        int last = Count - 1;

        if (index != last)
        {
            _values[index] = _values[last];
            MarkDirty(index);
        }

        Count--;
    }

    private bool IsDirty(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        return _dirtyBits![index];
    }

    private void MarkDirty(int index)
    {
        _dirtyBits?[index] = true;
    }

    private void MarkAllDirty()
    {
        if (_dirtyBits is not null)
            Array.Fill(_dirtyBits, true, 0, Count);
    }

    private void ClearDirty()
    {
        if (_dirtyBits is not null)
            Array.Clear(_dirtyBits, 0, Count);
    }

    private void EnsureCapacity(int count)
    {
        if (count <= Capacity)
            return;

        int newSize = Math.Max(count, Capacity * 2);
        Array.Resize(ref _values, newSize);

        if (_dirtyBits is not null)
            Array.Resize(ref _dirtyBits, newSize);
    }
}

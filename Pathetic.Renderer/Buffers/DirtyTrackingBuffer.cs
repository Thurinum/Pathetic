using System.Runtime.CompilerServices;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Buffers;

public sealed class DirtyTrackingBuffer<T>(string name) : IDynamicBufferSpec where T : unmanaged, IEquatable<T>
{
    private const int InitialSize = 256;
    
    private T[] _values = new T[InitialSize];
    private bool[] _dirtiness = new bool[InitialSize];

    public string Name { get; } = name;
    public ReadOnlySpan<T> Data => _values.AsSpan(0, Count);
    private int Capacity => _values.Length;
    public int Count { get; private set; }
    public uint Stride => (uint)Unsafe.SizeOf<T>();

    public uint ElementCount => (uint)Count;
    public IReadOnlyList<IResourceSpec> Resources => [this];

    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        if (resized)
        {
            MarkAllDirty();
        }

        for (int i = 0; i < Count; i++)
        {
            if (IsDirty(i))
            {
                ctx.Cmd.UpdateBuffer(buffer, (uint)i * Stride, Data[i]);
            }
        }

        ClearDirty();
    }

    public int Add(in T value)
    {
        EnsureCapacity(Count + 1);
        int index = Count++;
        _values[index] = value;
        _dirtiness[index] = true;
        return index;
    }

    public void UpdateAt(int index, in T value)
    {
        if (_values[index].Equals(value))
            return;

        _values[index] = value;
        _dirtiness[index] = true;
    }

    public void RemoveAt(int index)
    {
        int last = Count - 1;

        if (index != last)
        {
            _values[index] = _values[last];
            _dirtiness[index] = true;
        }

        Count--;
    }

    private bool IsDirty(int idx)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(idx);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idx, Count);
        
        return _dirtiness[idx];
    }
    
    private void MarkAllDirty()
    {
        Array.Fill(_dirtiness, true, 0, Count);
    }

    private void ClearDirty()
    {
        Array.Clear(_dirtiness, 0, Count);
    }

    private void EnsureCapacity(int count)
    {
        if (count <= Capacity) 
            return;
        
        int newSize = Math.Max(count, Capacity * 2);
        Array.Resize(ref _values, newSize);
        Array.Resize(ref _dirtiness, newSize);
    }
}

using System.Runtime.CompilerServices;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Buffers;

/// <summary>
/// A CPU-side array uploaded as a structured buffer.
/// </summary>
/// <param name="name">Resource name used to bind it from shaders.</param>
/// <param name="capacity">Element capacity requested on first allocation.</param>
/// <param name="resizable">
/// When false the GPU buffer never grows; overflowing <paramref name="capacity"/> throws instead.
/// </param>
/// <param name="deltaUpload">
/// When true only elements changed since the last upload are copied. When false the whole live
/// range is copied every frame, which is cheaper for data that always changes.
/// </param>
public sealed class StructuredBuffer<T>(
    string name,
    uint capacity = 256,
    bool resizable = true,
    bool deltaUpload = true) : IStructuredBufferSpec
    where T : unmanaged, IEquatable<T>
{
    private T[] _values = new T[CheckedCapacity(capacity)];
    private bool[]? _dirtiness = deltaUpload ? new bool[CheckedCapacity(capacity)] : null;

    public string Name { get; } = name;
    public ReadOnlySpan<T> Data => _values.AsSpan(0, Count);
    public int Count { get; private set; }
    public uint Stride => (uint)Unsafe.SizeOf<T>();
    public uint ElementCount => (uint)Count;
    public uint InitialCapacity => capacity;
    public bool IsResizable => resizable;
    public bool DeltaUpload => deltaUpload;
    public IReadOnlyList<IResourceSpec> Resources => [this];

    private int Capacity => _values.Length;

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
        if (_values[index].Equals(value))
            return;

        _values[index] = value;
        MarkDirty(index);
    }

    public void RemoveAt(int index)
    {
        int last = Count - 1;

        if (index != last)
        {
            _values[index] = _values[last];
            MarkDirty(index);
        }

        Count--;
    }

    private static int CheckedCapacity(uint capacity) =>
        capacity is > 0 and <= int.MaxValue
            ? (int)capacity
            : throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");

    private bool IsDirty(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        return _dirtiness![index];
    }

    private void MarkDirty(int index)
    {
        if (_dirtiness is not null)
            _dirtiness[index] = true;
    }

    private void MarkAllDirty()
    {
        if (_dirtiness is not null)
            Array.Fill(_dirtiness, true, 0, Count);
    }

    private void ClearDirty()
    {
        if (_dirtiness is not null)
            Array.Clear(_dirtiness, 0, Count);
    }

    private void EnsureCapacity(int count)
    {
        if (count <= Capacity)
            return;

        int newSize = Math.Max(count, Capacity * 2);
        Array.Resize(ref _values, newSize);

        if (_dirtiness is not null)
            Array.Resize(ref _dirtiness, newSize);
    }
}

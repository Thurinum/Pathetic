using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Buffers;

public sealed class StructuredBuffer<T> 
    : IStructuredBufferSpec where T : unmanaged, IEquatable<T>
{
    public string Name { get; }
    public int Count { get; private set; }
    public uint Stride => (uint)Unsafe.SizeOf<T>();
    public uint ElementCount => (uint)Count;
    public uint InitialCapacity { get; }
    public bool IsResizable { get; }
    public bool IsDynamic { get; }
    public IReadOnlyList<IResourceSpec> Resources => [this];
    public SceneChange DirtyChanges { get; private set; }

    private T[] _values;
    private int Capacity => _values.Length;
    private readonly BitArray _dirtyChunks;
    private readonly int _chunkSize;

    public StructuredBuffer(string name, bool resizable, bool dynamic, uint capacity, uint chunkSize)
    {
        Name = name;
        IsResizable = resizable;
        IsDynamic = dynamic;
        InitialCapacity = capacity;
        _values = new T[capacity];
        _chunkSize = (int)chunkSize;

        CheckCapacity();
        _dirtyChunks = new BitArray((int)GetChunkCount());
    }
    
    private uint GetChunkCount()
    {
        if (!IsPowerOfTwo(_chunkSize))
        {
            throw new InvalidOperationException("Structured buffer chunk size must be a power of two.");
        }
        
        // Capacity + (ChunkSize - 1) / ChunkSize
        // e.g. 64: 1000000 >> 6 == 0000001
        long count = (Capacity + _chunkSize - 1) >> BitOperations.TrailingZeroCount(_chunkSize);
        return (uint)count;
    }

    public void SetDirty(SceneChange changes)
    {
        DirtyChanges |= changes;
        MarkAllDirty();
    }

    public void ClearDirty()
    {
        DirtyChanges = SceneChange.None;
        _dirtyChunks.SetAll(false);
    }

    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        if (resized)
            MarkAllDirty();

        int c = 0;
        while (c < _dirtyChunks.Length)
        {
            if (!_dirtyChunks[c])
            {
                c++;
                continue;
            }

            int startChunkIdx = c;
            while (c < _dirtyChunks.Length && _dirtyChunks[c]) 
                c++;
            
            int startIdx = startChunkIdx * _chunkSize;
            if (startIdx > Count)
                break;
            
            int endIdx = Math.Min(c * _chunkSize, Count);

            var data = _values.AsSpan(startIdx, endIdx - startIdx);
            ctx.Cmd.UpdateBuffer(buffer, (uint)startIdx * Stride, data);
        }
    }

    public int Add(in T value)
    {
        EnsureCapacity(Count + 1);
        int index = Count++;
        
        _values[index] = value;
        DirtyChanges |= SceneChange.Structure | SceneChange.Content;
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
        DirtyChanges |= SceneChange.Content;
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
        }

        MarkDirty(index);
        DirtyChanges |= SceneChange.Structure;
        Count--;
    }
    
    // e.g. 64: 1000000 & 0111111 == 0
    private static bool IsPowerOfTwo(long n)
    {
        if (n == 0)
            throw new DivideByZeroException();
        
        return (n & (n - 1)) == 0;
    }
    
    private void CheckCapacity()
    {
        if (!IsPowerOfTwo(Capacity))
        {
            throw new InvalidOperationException("Structured buffer capacity must be a power of two.");
        }
    }
    
    private void EnsureCapacity(int count)
    {
        if (count <= Capacity)
            return;

        int newSize = Math.Max(count, Capacity * 2);
        Array.Resize(ref _values, newSize);
        _dirtyChunks.Length = (int)GetChunkCount();
    }

    private void MarkDirty(int index)
    {
        int c = index / _chunkSize;
        _dirtyChunks[c] = true;
    }

    private void MarkAllDirty()
    {
        _dirtyChunks.SetAll(true);
    }
}

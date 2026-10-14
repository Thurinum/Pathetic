using System.Runtime.CompilerServices;
using NeoVeldrid;

namespace Pathetic.Renderer.Primitives;

public sealed class PrimitiveBuffer<T>(string name) : IPrimitiveBuffer where T : unmanaged, IEquatable<T>
{
    private const int InitialSize = 256;
    
    private T[] _primitives = new T[InitialSize];
    private bool[] _dirtiness = new bool[InitialSize];
    private int[] _versions = new int[InitialSize];
    private int[] _handleIdxToDenseIdx = new int[InitialSize];
    private int[] _denseIdxToHandleIdx = new int[InitialSize];
    private readonly Stack<int> _freeHandles = new();

    public string Name { get; } = name;
    public Type PrimitiveType => typeof(T);
    public ReadOnlySpan<T> Data => _primitives.AsSpan(0, Count);
    public int Capacity => _primitives.Length;
    public int Count { get; private set; }
    public uint Stride => (uint)Unsafe.SizeOf<T>();

    public uint ElementCount => (uint)Count;

    public void Upload(CommandList cmd, DeviceBuffer buf, bool resized)
    {
        if (resized)
        {
            MarkAllDirty();
        }

        for (int i = 0; i < Count; i++)
        {
            if (IsDirty(i))
            {
                cmd.UpdateBuffer(buf, (uint)i * Stride, Data[i]);
            }
        }

        ClearDirty();
    }

    public PrimitiveHandle Add(in T primitive)
    {
        EnsureCapacity(Count + 1);
        int denseIdx = Count++;

        int hndlIdx = _freeHandles.Count > 0 ? _freeHandles.Pop() : denseIdx;
        
        _primitives[denseIdx] = primitive;
        _dirtiness[denseIdx] = true;
        _handleIdxToDenseIdx[hndlIdx] = denseIdx;
        _denseIdxToHandleIdx[denseIdx] = hndlIdx;
        
        return new PrimitiveHandle(hndlIdx, _versions[hndlIdx]);
    }

    public void Update(PrimitiveHandle hndl, in T primitive)
    {
        if (!hndl.IsValid || hndl.Version != _versions[hndl.Index])
            throw new InvalidOperationException("Invalid or outdated primitive handle.");
        
        int denseIdx = _handleIdxToDenseIdx[hndl.Index];
        if (_primitives[denseIdx].Equals(primitive))
            return;
        
        _primitives[denseIdx] = primitive;
        _dirtiness[denseIdx] = true;
    }

    public void Remove(ref PrimitiveHandle hndl)
    {
        if (!hndl.IsValid || hndl.Version != _versions[hndl.Index])
            throw new InvalidOperationException("Invalid or outdated primitive handle.");
        
        int denseIdx = _handleIdxToDenseIdx[hndl.Index];
        int lastIdx = Count - 1;

        if (denseIdx != lastIdx)
        {
            int movedHndlIdx = _denseIdxToHandleIdx[lastIdx];
            (_primitives[lastIdx], _primitives[denseIdx]) = (_primitives[denseIdx], _primitives[lastIdx]);
            _dirtiness[denseIdx] = true; // dirty the moved element
            _handleIdxToDenseIdx[movedHndlIdx] = denseIdx;
            _denseIdxToHandleIdx[denseIdx] = movedHndlIdx;
        }
        
        Count--;
        _handleIdxToDenseIdx[hndl.Index] = -1;
        _versions[hndl.Index]++;
        _freeHandles.Push(hndl.Index);
        hndl = PrimitiveHandle.Invalid;
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
        Array.Resize(ref _primitives, newSize);
        Array.Resize(ref _dirtiness, newSize);
        Array.Resize(ref _versions, newSize);
        Array.Resize(ref _handleIdxToDenseIdx, newSize);
        Array.Resize(ref _denseIdxToHandleIdx, newSize);
    }
}
namespace Pathetic.Renderer.Buffers;

public sealed class SparseSet<T>(StructuredBuffer<T> buffer) where T : unmanaged, IEquatable<T>
{
    private const int InitialSize = 256;

    private int[] _versions = new int[InitialSize];
    private int[] _handleIdxToDenseIdx = new int[InitialSize];
    private int[] _denseIdxToHandleIdx = new int[InitialSize];
    private readonly Stack<int> _freeHandles = new();

    public Handle Add(in T value)
    {
        int denseIdx = buffer.Add(value);
        EnsureCapacity(denseIdx + 1);

        int hndlIdx = _freeHandles.Count > 0 ? _freeHandles.Pop() : denseIdx;

        _handleIdxToDenseIdx[hndlIdx] = denseIdx;
        _denseIdxToHandleIdx[denseIdx] = hndlIdx;

        return new Handle(hndlIdx, _versions[hndlIdx]);
    }

    public void Update(Handle handle, in T value)
    {
        if (!IsValid(handle))
            throw new InvalidOperationException("Invalid or outdated handle.");

        buffer.UpdateAt(_handleIdxToDenseIdx[handle.Index], value);
    }

    public void Remove(ref Handle handle)
    {
        if (!IsValid(handle))
            throw new InvalidOperationException("Invalid or outdated handle.");

        int denseIdx = _handleIdxToDenseIdx[handle.Index];
        int lastIdx = buffer.Count - 1;
        int movedHndlIdx = _denseIdxToHandleIdx[lastIdx];

        buffer.RemoveAt(denseIdx);

        if (denseIdx != lastIdx)
        {
            _handleIdxToDenseIdx[movedHndlIdx] = denseIdx;
            _denseIdxToHandleIdx[denseIdx] = movedHndlIdx;
        }

        _handleIdxToDenseIdx[handle.Index] = -1;
        _versions[handle.Index]++;
        _freeHandles.Push(handle.Index);
        handle = Handle.Invalid;
    }

    public bool IsValid(Handle handle)
    {
        return handle.IsValid && handle.Version == _versions[handle.Index];
    }

    private void EnsureCapacity(int handleCount)
    {
        if (handleCount <= _versions.Length)
            return;

        int newSize = Math.Max(handleCount, _versions.Length * 2);
        Array.Resize(ref _versions, newSize);
        Array.Resize(ref _handleIdxToDenseIdx, newSize);
        Array.Resize(ref _denseIdxToHandleIdx, newSize);
    }
}

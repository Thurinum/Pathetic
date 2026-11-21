using System.Runtime.CompilerServices;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Buffers;

public sealed class SnapshotBuffer<T>(string name, uint capacity = 256) : IStructuredBufferSpec
    where T : unmanaged
{
    private T[] _values = new T[capacity];
    private int _count;

    public string Name { get; } = name;
    public uint Stride => (uint)Unsafe.SizeOf<T>();
    public uint ElementCount => (uint)_count;
    public uint InitialCapacity => capacity;
    public bool IsResizable => true;
    public bool IsDynamic => true;

    public IReadOnlyList<IResourceSpec> Resources => [this];
    public SceneChange DirtyChanges { get; private set; }

    public void SetDirty(SceneChange changes) => DirtyChanges |= changes;
    public void ClearDirty() => DirtyChanges = SceneChange.None;

    public void Set(IReadOnlyList<T> values)
    {
        EnsureCapacity(values.Count);

        for (int i = 0; i < values.Count; i++)
        {
            _values[i] = values[i];
        }

        _count = values.Count;
        DirtyChanges |= SceneChange.Structure | SceneChange.Content;
    }

    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        if (_count == 0)
            return;

        ctx.Cmd.UpdateBuffer(buffer, 0, _values.AsSpan(0, _count));
    }

    private void EnsureCapacity(int count)
    {
        if (count > _values.Length)
            Array.Resize(ref _values, Math.Max(count, _values.Length * 2));
    }
}

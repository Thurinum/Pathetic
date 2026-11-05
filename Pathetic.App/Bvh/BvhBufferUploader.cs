using NeoVeldrid;
using Pathetic.Renderer.Buffers;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.Bvh;

public sealed class BvhBufferUploader<T>(
    string name,
    SceneBvh sceneBvh,
    Func<SceneBvh, T[]> selector) : IResourceUploader, IStructuredBufferSpec
    where T : unmanaged, IEquatable<T>
{
    private readonly StructuredBuffer<T> _buffer = new(name, resizable: true, dynamic: true, capacity: 256, chunkSize: 64);
    private uint _lastVersion;

    public string Name => _buffer.Name;
    public uint Stride => _buffer.Stride;
    public uint ElementCount => _buffer.ElementCount;
    public uint InitialCapacity => _buffer.InitialCapacity;
    public bool IsResizable => _buffer.IsResizable;
    public bool IsDynamic => _buffer.IsDynamic;
    public IReadOnlyList<IResourceSpec> Resources => [this];

    public SceneChange DirtyChanges =>
        sceneBvh.IsDirty || _lastVersion != sceneBvh.Version || _buffer.DirtyChanges != SceneChange.None
            ? SceneChange.Content
            : SceneChange.None;

    public void SetDirty(SceneChange changes) => _buffer.SetDirty(changes);
    public void ClearDirty() => _buffer.ClearDirty();

    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        sceneBvh.EnsureBuilt();

        if (_lastVersion != sceneBvh.Version)
        {
            _buffer.SetAll(selector(sceneBvh));
            _lastVersion = sceneBvh.Version;
        }

        _buffer.Upload(ctx, buffer, resized);
    }
}

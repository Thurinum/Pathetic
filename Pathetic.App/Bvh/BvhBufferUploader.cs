using System.Runtime.CompilerServices;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.Bvh;

public sealed class BvhBufferUploader<T>(
    string name,
    SceneBvh sceneBvh,
    Func<SceneBvh, T[]> selector) : IResourceUploader, IStructuredBufferSpec
    where T : unmanaged
{
    private T[] _data = [];
    private uint _uploadedVersion = uint.MaxValue;
    private bool _dirty = true;

    public string Name => name;
    public uint Stride => (uint)Unsafe.SizeOf<T>();
    public uint ElementCount
    {
        get
        {
            Refresh();
            return (uint)_data.Length;
        }
    }

    public uint InitialCapacity => 256;
    public bool IsResizable => true;
    public bool IsDynamic => true;
    public IReadOnlyList<IResourceSpec> Resources => [this];

    public SceneChange DirtyChanges
    {
        get
        {
            Refresh();
            return _dirty ? SceneChange.Content : SceneChange.None;
        }
    }

    public void SetDirty(SceneChange changes) => _dirty |= changes != SceneChange.None;
    public void ClearDirty() => _dirty = false;

    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        Refresh();

        if (_data.Length > 0)
            ctx.Cmd.UpdateBuffer(buffer, 0, _data);

        _dirty = false;
    }

    private void Refresh()
    {
        if (!sceneBvh.IsDirty && _uploadedVersion == sceneBvh.Version)
            return;

        if (sceneBvh.IsDirty)
            sceneBvh.Rebuild();

        _data = selector(sceneBvh);
        _uploadedVersion = sceneBvh.Version;
        _dirty = true;
    }
}

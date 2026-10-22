using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.Components.Camera;

public sealed class CameraProvider : IResourceUploader
{
    private CameraData _data;
    private SceneChange _dirty = SceneChange.All;

    public void Set(in CameraData data)
    {
        if (data.Equals(_data))
            return;

        _data = data;
        SetDirty(SceneChange.Content);
    }

    public IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        UniformBufferSpec.Of<CameraData>(ResourceName.Camera)
    ];

    public SceneChange DirtyChanges => _dirty;
    public void SetDirty(SceneChange changes) => _dirty |= changes;
    public void ClearDirty() => _dirty = SceneChange.None;

    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ctx.Cmd.UpdateBuffer(buffer, 0, _data);
    }
}

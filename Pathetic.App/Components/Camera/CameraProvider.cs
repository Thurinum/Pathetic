using System.Numerics;
using NeoVeldrid;
using Pathetic.App.RenderPasses;
using Pathetic.Renderer;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.Components.Camera;

public sealed class CameraProvider(RenderWindow window) : IResourceUploader
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

    public Ray ScreenToCamera(Vector2 screenPosition01)
    {
        if (_data.TanHalfFOV <= 0f)
            return new Ray(_data.Origin, -Vector3.UnitZ);

        float aspect = window.Height > 0 ? window.Width / (float)window.Height : 1f;
        float viewPlaneX = (2f * screenPosition01.X - 1f) * aspect * _data.TanHalfFOV;
        float viewPlaneY = (1f - 2f * screenPosition01.Y) * _data.TanHalfFOV;

        Vector3 direction = _data.Forward + viewPlaneX * _data.Right + viewPlaneY * _data.Up;
        return new Ray(_data.Origin, Vector3.Normalize(direction));
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

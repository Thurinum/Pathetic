using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.Components.Camera;

public class CameraProvider(SceneRevision revision) : IResourceUploader
{
    private CameraData _data;

    public void Set(in CameraData data)
    {
        if (data.Equals(_data))
            return;

        _data = data;
        revision.Invalidate();
    }

    public IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        UniformBufferSpec.Of<CameraData>(ResourceName.Camera)    
    ];
    
    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ctx.Cmd.UpdateBuffer(buffer, 0, _data);
    }
}
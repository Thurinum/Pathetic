using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.Components;

public sealed class FrameStateProvider : IResourceProvider, IResourceUploader
{
    public IEnumerable<IResourceSpec> Resources { get; } =
    [
        UniformBufferSpec.Of<FrameState>(Pathetic.App.Resources.FrameState)
    ];

    public void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        ctx.Cmd.UpdateBuffer(buffer, 0, new FrameState
        {
            FrameIndex = ctx.FrameIndex,
            DeltaTime = ctx.DeltaTime
        });
    }
}

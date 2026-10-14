using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

// Edge-detects the object id buffer to draw an outline around the active object, after accumulation
// so the outline is never accumulated or denoised. Generalises to any geometry the id pass can hit.
public sealed class SelectionOutlinePass(SelectionState selection) : RenderPass
{
    private struct Params
    {
        public uint SelectedObjectId;
    }

    public override string ShaderModule => "SelectionOutline";

    public override IReadOnlyList<IResourceSpec> Outputs { get; } =
    [
        new TextureSpec(Resources.ColorOutlined, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Params>(Resources.Params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        Resources.Color,
        Resources.ObjectId,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        Params @params = new() { SelectedObjectId = selection.SelectedObjectId };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

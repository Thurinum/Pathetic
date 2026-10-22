using Microsoft.Extensions.Options;
using NeoVeldrid;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class TemporalAccumulationPass(IOptions<TemporalAccumulationOptions> options) : RenderPass
{
    private struct Params
    {
        public uint Reset;
    }

    public override string ShaderModule => "TemporalAccumulation";

    public override IReadOnlyList<IResourceSpec> Resources { get; } =
    [
        new TextureSpec(ResourceName.PresentColor, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        new TextureSpec(ResourceName.AccumImage, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Params>(ResourceName.Params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        ResourceName.PathTracerColor,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        bool reset = !options.Value.Enabled || ctx.Resources.SceneRevision != SceneChange.None;

        ctx.Cmd.UpdateBuffer(buffer, 0, new Params { Reset = reset ? 1u : 0u });
    }
}

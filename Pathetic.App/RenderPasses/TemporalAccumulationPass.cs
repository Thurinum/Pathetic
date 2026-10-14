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

    private uint _lastRevision = uint.MaxValue;
    
    public override string ShaderModule => "TemporalAccumulation";

    public override IReadOnlyList<IResourceSpec> Outputs { get; } =
    [
        new TextureSpec(Resources.Color, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        new TextureSpec(Resources.AccumImage, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage, new AutoSize()),
        UniformBufferSpec.Of<Params>(Resources.Params),
    ];

    public override IReadOnlyList<string> Inputs { get; } =
    [
        Resources.PathTracerColor,
    ];

    public override void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized)
    {
        bool reset = !options.Value.Enabled || ctx.SceneRevision != _lastRevision;
        _lastRevision = ctx.SceneRevision;

        Params @params = new() { Reset = reset ? 1u : 0u };
        ctx.Cmd.UpdateBuffer(buffer, 0, @params);
    }
}

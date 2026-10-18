using NeoVeldrid;
using Pathetic.Renderer.Resources;
using Pathetic.Renderer.Utils;

namespace Pathetic.Renderer.Graph;

public abstract class RenderPass : IResourceUploader
{
    public bool Enabled { get; set; } = true;
    public abstract string ShaderModule { get; }

    public virtual IReadOnlyList<IResourceSpec> Resources => [];
    public virtual IReadOnlyList<string> Inputs => [];

    public virtual void Upload(RenderContext ctx, DeviceBuffer buffer, bool resized) {}
    protected virtual (uint x, uint y) DispatchSize(RenderContext ctx) => (ctx.Width, ctx.Height);

    public void Execute(RenderContext ctx, PassRenderState state)
    {
        ctx.Cmd.SetPipeline(state.Pipeline);
        ctx.Cmd.SetComputeResourceSet(0, state.Set);
        (uint w, uint h) = DispatchSize(ctx);
        (uint x, uint y, uint z) = ComputeThreadGroupCount(state.ThreadGroupSize, w, h);
        ctx.Cmd.Dispatch(x, y, z);
    }
    
    private static UIntVector3 ComputeThreadGroupCount(UIntVector3 groupSize, uint width, uint height)
    {
        uint x = (width + groupSize.X - 1) / groupSize.X;
        uint y = (height + groupSize.Y - 1) / groupSize.Y;
        return (x, y, 1);
    }
}

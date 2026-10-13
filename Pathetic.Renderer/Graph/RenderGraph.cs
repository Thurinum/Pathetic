using NeoVeldrid;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Graph;

internal sealed class PassEntry(RenderPass pass, PassRenderState state)
{
    public RenderPass Pass { get; } = pass;
    public PassRenderState State { get; } = state;
}

internal sealed class RenderGraph(
    ResourceTable resources,
    List<PassEntry> passes,
    string texture,
    SceneRevision sceneRevision,
    IEnumerable<IResourceProvider> resourceProviders) : IDisposable
{
    private uint _frameIndex;

    public void Render(GraphicsDevice device, CommandList cmd, float deltaTime)
    {
        var target = device.SwapchainFramebuffer.ColorTargets[0].Target;
        RenderContext ctx = new()
        {
            Cmd = cmd,
            Resources = resources,
            Target = target,
            Width = target.Width,
            Height = target.Height,
            DeltaTime = deltaTime,
            FrameIndex = _frameIndex,
            SceneRevision = sceneRevision.Value
        };

        var reallocatedBindingNames = resources.Reallocate(device, ctx.Cmd);
        foreach (var entry in GetPassesWithBindings(reallocatedBindingNames))
        {
            RenderGraphPipelineFactory.UpdateResourceSet(entry.State, entry.Pass, device, resources);
        }

        foreach (var provider in resourceProviders)
        {
            provider.Update(ctx);
        }

        foreach (var entry in passes)
        {
            if (entry.Pass.Enabled)
            {
                entry.Pass.Execute(ctx, entry.State);
            }
        }

        var presentTexture = resources.GetResource<Texture>(texture);
        ctx.Cmd.CopyTexture(presentTexture, ctx.Target);

        _frameIndex++;
    }

    public void Resize(GraphicsDevice device, uint width, uint height)
    {
        sceneRevision.Invalidate();
        var resizedTextureNames = resources.Resize(device, width, height);

        foreach (var entry in GetPassesWithBindings(resizedTextureNames))
        {
            RenderGraphPipelineFactory.UpdateResourceSet(entry.State, entry.Pass, device, resources);
        }
    }

    public void Dispose()
    {
        foreach (var entry in passes)
        {
            entry.State.Dispose();
        }

        resources.Dispose();
    }

    private IEnumerable<PassEntry> GetPassesWithBindings(IReadOnlySet<string> bindingNames)
    {
        if (bindingNames.Count > 0)
        {
            return passes.Where(p => p.State.Bindings.Any(b => bindingNames.Contains(b.Name)));
        }

        return [];
    }
}

using NeoVeldrid;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Graph;

internal sealed class PassEntry(RenderPass pass, PassRenderState state)
{
    public RenderPass Pass { get; } = pass;
    public PassRenderState State { get; } = state;
}

public sealed class RenderGraph : IDisposable
{
    private readonly ResourceTable _resources;
    private readonly List<PassEntry> _passes;
    private readonly string _texture;
    private uint _frameIndex;
    private bool _disposed;

    internal RenderGraph(ResourceTable resources, List<PassEntry> passes, string texture)
    {
        _resources = resources;
        _passes = passes;
        _texture = texture;
    }

    public Texture PresentTexture => _resources.GetResource<Texture>(_texture);

    public void Render(
        GraphicsDevice device,
        CommandList cmd,
        float deltaTime,
        uint width,
        uint height,
        Texture? target = null)
    {
        RenderContext ctx = new()
        {
            Cmd = cmd,
            Width = width,
            Height = height,
            DeltaTime = deltaTime,
            FrameIndex = _frameIndex,
            Resources = _resources
        };

        var reallocatedBindingNames = _resources.Upload(device, ctx);
        foreach (var entry in GetPassesWithBindings(reallocatedBindingNames))
        {
            RenderGraphPipelineFactory.UpdateResourceSet(entry.State, entry.Pass, device, _resources);
        }

        foreach (var entry in _passes)
        {
            if (entry.Pass.Enabled)
            {
                entry.Pass.Execute(ctx, entry.State);
            }
        }

        if (target is not null)
            ctx.Cmd.CopyTexture(PresentTexture, target);

        _frameIndex++;
    }

    public void Resize(GraphicsDevice device, uint width, uint height)
    {
        var resizedTextureNames = _resources.Resize(device, width, height);

        foreach (var entry in GetPassesWithBindings(resizedTextureNames))
        {
            RenderGraphPipelineFactory.UpdateResourceSet(entry.State, entry.Pass, device, _resources);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var entry in _passes)
        {
            entry.State.Dispose();
        }

        _resources.Dispose();
    }

    private IEnumerable<PassEntry> GetPassesWithBindings(IReadOnlySet<string> bindingNames)
    {
        if (bindingNames.Count > 0)
        {
            return _passes.Where(p => p.State.Bindings.Any(b => bindingNames.Contains(b.Name)));
        }

        return [];
    }
}

using NeoVeldrid;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Graph;

public class PassEntry(RenderPass pass)
{
    public RenderPass Pass = pass;
    public PassRenderState? State;
}

public sealed class RenderGraph(
    RenderGraphBuilder graphBuilder,
    RenderPassFactory passFactory,
    RenderGraphPipelineFactory stateFactory,
    SceneRevision sceneRevision,
    IEnumerable<IResourceProvider> resourceProviders) : IDisposable
{
    private readonly ResourceTable _resources = new();
    private readonly List<PassEntry> _passes = [];
    private uint _frameIndex = 0;

    public T? GetPass<T>() where T : RenderPass => _passes.Select(e => e.Pass).OfType<T>().SingleOrDefault();

    public void Build(GraphicsDevice device)
    {
        CreatePasses();
        CreateResources(device, device.SwapchainFramebuffer);
        ValidatePresentTexture(device.SwapchainFramebuffer);
        ValidatePassInputs();
        BuildPassPipelines(device);
    }

    public void Render(GraphicsDevice device, CommandList cmd, float deltaTime)
    {
        var target = device.SwapchainFramebuffer.ColorTargets[0].Target;
        RenderContext ctx = new()
        {
            Cmd = cmd,
            Resources = _resources,
            Target = target,
            Width = target.Width,
            Height = target.Height,
            DeltaTime = deltaTime,
            FrameIndex = _frameIndex,
            SceneRevision = sceneRevision.Value
        };
        
        var reallocatedBindingNames = _resources.Reallocate(device, ctx.Cmd);
        foreach (var entry in GetPassesWithBindings(reallocatedBindingNames))
        {
            stateFactory.UpdateResourceSet(entry.State!, entry.Pass, device, _resources);
        }
        
        foreach (var provider in resourceProviders)
        {
            provider.Update(ctx);
        }
        
        foreach (var entry in _passes)
        {
            if (entry.Pass.Enabled)
            {
                entry.Pass.Execute(ctx, entry.State!);
            }
        }

        var presentTexture = _resources.Get<Texture>(graphBuilder.PresentTextureName!);
        ctx.Cmd.CopyTexture(presentTexture, ctx.Target);

        _frameIndex++;
    }

    public void Resize(GraphicsDevice device, uint width, uint height)
    {
        sceneRevision.Invalidate();
        var resizedTextureNames = _resources.Resize(device, width, height);
        
        foreach (var entry in GetPassesWithBindings(resizedTextureNames))
        {
            stateFactory.UpdateResourceSet(entry.State!, entry.Pass, device, _resources);
        }
    }

    public void Dispose()
    {
        foreach (var entry in _passes)
        {
            entry.State?.Dispose();
        }
        
        _resources.Dispose();
    }

    private void CreatePasses()
    {
        foreach (var passType in graphBuilder.PassTypes)
        {
            var pass = passFactory.CreateRenderPass(passType);
            _passes.Add(new PassEntry(pass));
        }
    }

    private void CreateResources(GraphicsDevice device, Framebuffer framebuffer)
    {
        foreach (var spec in resourceProviders.SelectMany(p => p.Resources))
        {
            _resources.Declare(spec);
        }
        
        foreach (var entry in _passes)
        {
            foreach (var output in entry.Pass.Outputs)
            {
                var resource = output is UniformBufferSpec ? entry.Pass : null;
                _resources.Declare(output, resource);
            }
            foreach (var sampler in entry.Pass.Samplers)
            {
                _resources.Declare(sampler);
            }
        }

        _resources.Allocate(device, framebuffer.Width, framebuffer.Height);
    }

    private void ValidatePresentTexture(Framebuffer framebuffer)
    {
        if (graphBuilder.PresentTextureName == null)
            throw new InvalidOperationException("No present texture is set.");
        
        if (!_resources.Has<Texture>(graphBuilder.PresentTextureName))
            throw new InvalidOperationException($"Present texture {graphBuilder.PresentTextureName} isn't a valid resource.");
        
        if (_resources.Get<Texture>(graphBuilder.PresentTextureName).Format != framebuffer.ColorTargets[0].Target.Format)
            throw new InvalidOperationException($"Present tex {graphBuilder.PresentTextureName} must have same format as the swapchain.");
    }

    private void ValidatePassInputs()
    {
        foreach (var entry in _passes)
        {
            foreach (var input in entry.Pass.Inputs)
            {
                if (!_resources.Has(input.Name))
                {
                    throw new InvalidOperationException($"Pass {entry.Pass.GetType().Name} has invalid input {input.Name}");
                }
            }
        }
    }

    private void BuildPassPipelines(GraphicsDevice device)
    {
        foreach (var entry in _passes)
        {
            entry.State = stateFactory.Build(device, entry.Pass, _resources);
        }
    }

    private IEnumerable<PassEntry> GetPassesWithBindings(IReadOnlySet<string> bindingNames)
    {
        if (bindingNames.Count > 0)
        {
            return _passes.Where(p => p.State!.Bindings.Any(b => bindingNames.Contains(b.Name)));
        }

        return [];
    } 
}

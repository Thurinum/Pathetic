using NeoVeldrid;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Graph;

public sealed class PassEntry(RenderPass pass)
{
    public RenderPass Pass { get; } = pass;
    public PassRenderState? State { get; set; }
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
    private uint _frameIndex;

    public void Build(GraphicsDevice device)
    {
        CreatePasses();
        CreateResources(device, device.SwapchainFramebuffer);
        ValidatePresentTexture(device.SwapchainFramebuffer);
        BuildPassPipelines(device);
        ValidatePassBindings();
        BindPassResources(device);
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
            RenderGraphPipelineFactory.UpdateResourceSet(entry.State!, entry.Pass, device, _resources);
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

        var presentTexture = _resources.GetResource<Texture>(graphBuilder.PresentTextureName!);
        ctx.Cmd.CopyTexture(presentTexture, ctx.Target);

        _frameIndex++;
    }

    public void Resize(GraphicsDevice device, uint width, uint height)
    {
        sceneRevision.Invalidate();
        var resizedTextureNames = _resources.Resize(device, width, height);
        
        foreach (var entry in GetPassesWithBindings(resizedTextureNames))
        {
            RenderGraphPipelineFactory.UpdateResourceSet(entry.State!, entry.Pass, device, _resources);
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
        
        if (_resources.GetResource<Texture>(graphBuilder.PresentTextureName).Format != framebuffer.ColorTargets[0].Target.Format)
            throw new InvalidOperationException($"Present tex {graphBuilder.PresentTextureName} must have same format as the swapchain.");
    }

    private void ValidatePassBindings()
    {
        foreach (var entry in _passes)
        {
            string passName = entry.Pass.GetType().Name;
            string shader = entry.Pass.ShaderModule;
            var bindings = entry.State!.Bindings;

            foreach (var input in entry.Pass.Inputs)
            {
                if (!_resources.Has(input))
                {
                    throw new InvalidOperationException($"Pass {passName} has invalid input {input}.");
                }

                if (bindings.All(b => b.Name != input))
                {
                    throw new InvalidOperationException($"Pass {passName} declares input '{input}' that shader '{shader}' does not consume.");
                }
            }

            foreach (var sampler in entry.Pass.Samplers)
            {
                if (bindings.All(b => b.Name != sampler.Name))
                {
                    throw new InvalidOperationException($"Pass {passName} declares sampler '{sampler.Name}' that shader '{shader}' does not consume.");
                }

                ResourceKind reflectedKind = bindings.First(b => b.Name == sampler.Name).Kind;
                if (reflectedKind != ResourceKind.Sampler)
                {
                    throw new InvalidOperationException($"Pass {passName} sampler '{sampler.Name}': shader '{shader}' declares {reflectedKind}, expected {ResourceKind.Sampler}.");
                }
            }
            
            var declared = entry.Pass.Inputs
                .Concat(entry.Pass.Outputs.Select(o => o.Name))
                .Concat(entry.Pass.Samplers.Select(s => s.Name))
                .ToHashSet(StringComparer.Ordinal);

            foreach (var binding in bindings)
            {
                if (!declared.Contains(binding.Name))
                {
                    throw new InvalidOperationException($"Pass {passName} shader '{shader}' binds '{binding.Name}', which the pass does not declare as an Input, Output, or Sampler.");
                }
            }
        }
    }

    private void BuildPassPipelines(GraphicsDevice device)
    {
        foreach (var entry in _passes)
        {
            entry.State = stateFactory.Build(device, entry.Pass);
        }
    }

    private void BindPassResources(GraphicsDevice device)
    {
        foreach (var entry in _passes)
        {
            RenderGraphPipelineFactory.UpdateResourceSet(entry.State!, entry.Pass, device, _resources);
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

using NeoVeldrid;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Graph;

public sealed class RenderGraphFactory(
    RenderPassFactory passFactory,
    RenderGraphPipelineFactory pipelineFactory,
    SceneRevision sceneRevision,
    IEnumerable<IResourceProvider> resourceProviders)
{
    internal RenderGraph Create(RenderGraphSpec spec, GraphicsDevice device)
    {
        string presentTexture = RenderGraphValidator.ValidateSpec(spec);

        var resources = new ResourceTable();
        var passes = CreatePasses(spec, device);

        var framebuffer = device.SwapchainFramebuffer;
        CreateResources(device, framebuffer, resources, passes);
        RenderGraphValidator.ValidatePresentTexture(presentTexture, resources, framebuffer);
        RenderGraphValidator.ValidatePassBindings(passes, resources);
        BindPassResources(device, passes, resources);

        return new RenderGraph(resources, passes, presentTexture, sceneRevision, resourceProviders);
    }

    private List<PassEntry> CreatePasses(RenderGraphSpec spec, GraphicsDevice device)
    {
        var passes = new List<PassEntry>(spec.PassTypes.Count);

        foreach (var passType in spec.PassTypes)
        {
            var pass = passFactory.CreateRenderPass(passType);
            var state = pipelineFactory.Build(device, pass);
            passes.Add(new PassEntry(pass, state));
        }

        return passes;
    }

    private void CreateResources(
        GraphicsDevice device,
        Framebuffer framebuffer,
        ResourceTable resources,
        List<PassEntry> passes)
    {
        foreach (var resourceSpec in resourceProviders.SelectMany(p => p.Resources))
        {
            resources.Declare(resourceSpec);
        }

        foreach (var entry in passes)
        {
            foreach (var output in entry.Pass.Outputs)
            {
                var owner = output is UniformBufferSpec ? entry.Pass : null;
                resources.Declare(output, owner);
            }

            foreach (var sampler in entry.Pass.Samplers)
            {
                resources.Declare(sampler);
            }
        }

        resources.Allocate(device, framebuffer.Width, framebuffer.Height);
    }

    private static void BindPassResources(GraphicsDevice device, List<PassEntry> passes, ResourceTable resources)
    {
        foreach (var entry in passes)
        {
            RenderGraphPipelineFactory.UpdateResourceSet(entry.State, entry.Pass, device, resources);
        }
    }
}

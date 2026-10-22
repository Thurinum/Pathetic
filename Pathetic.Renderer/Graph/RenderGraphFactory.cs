using NeoVeldrid;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Graph;

public sealed class RenderGraphFactory(
    RenderPassFactory passFactory,
    RenderGraphPipelineFactory pipelineFactory,
    IEnumerable<IResourceUploader> resourceUploaders)
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

        return new RenderGraph(resources, passes, presentTexture);
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
        IEnumerable<IResourceUploader> uploaders = resourceUploaders.Concat(passes.Select(IResourceUploader (entry) => entry.Pass));

        foreach (var uploader in uploaders)
        {
            foreach (var spec in uploader.Resources)
            {
                var owner = uploader is RenderPass pass && spec is UniformBufferSpec ? pass : null;
                resources.Declare(spec, owner, writer: uploader);
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

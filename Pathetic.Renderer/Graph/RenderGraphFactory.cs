using NeoVeldrid;
using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Graph;

public sealed class RenderGraphFactory(
    RenderPassFactory passFactory,
    RenderGraphPipelineFactory pipelineFactory,
    IEnumerable<IResourceUploader> resourceUploaders)
{
    public RenderGraph Create(
        RenderGraphSpec spec,
        GraphicsDevice device,
        uint width,
        uint height,
        PixelFormat presentFormat)
    {
        string presentTexture = RenderGraphValidator.ValidateSpec(spec);

        var resources = new ResourceTable();
        var passes = CreatePasses(spec, device);

        CreateResources(device, width, height, resources, passes);
        RenderGraphValidator.ValidatePresentTexture(presentTexture, resources, presentFormat);
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
        uint width,
        uint height,
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

        resources.Allocate(device, width, height);
    }

    private static void BindPassResources(GraphicsDevice device, List<PassEntry> passes, ResourceTable resources)
    {
        foreach (var entry in passes)
        {
            RenderGraphPipelineFactory.UpdateResourceSet(entry.State, entry.Pass, device, resources);
        }
    }
}

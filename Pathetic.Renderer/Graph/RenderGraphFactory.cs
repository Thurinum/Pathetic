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
        CreateResources(device, framebuffer, resources, passes, presentTexture);
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
        List<PassEntry> passes,
        string presentTexture)
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

        resources.SetVisibleResources(FindVisibleResources(presentTexture, passes));

        resources.Allocate(device, framebuffer.Width, framebuffer.Height);
    }
    
    private static HashSet<string> FindVisibleResources(string presentTexture, List<PassEntry> passes)
    {
        var producers = new Dictionary<string, RenderPass>(StringComparer.Ordinal);

        foreach (var entry in passes)
        {
            foreach (var spec in entry.Pass.Resources)
            {
                producers.TryAdd(spec.Name, entry.Pass);
            }
        }

        var visible = new HashSet<string>(StringComparer.Ordinal) { presentTexture };
        var pending = new Queue<string>();
        pending.Enqueue(presentTexture);

        while (pending.Count > 0)
        {
            if (!producers.TryGetValue(pending.Dequeue(), out RenderPass? producer))
                continue;

            foreach (string input in producer.Inputs)
            {
                if (visible.Add(input))
                    pending.Enqueue(input);
            }
        }

        return visible;
    }

    private static void BindPassResources(GraphicsDevice device, List<PassEntry> passes, ResourceTable resources)
    {
        foreach (var entry in passes)
        {
            RenderGraphPipelineFactory.UpdateResourceSet(entry.State, entry.Pass, device, resources);
        }
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Pathetic.Renderer.Graph;

public class RenderPassFactory(IServiceProvider provider)
{
    public RenderPass CreateRenderPass(Type type)
    {
        return (RenderPass)ActivatorUtilities.CreateInstance(provider, type);
    }
}

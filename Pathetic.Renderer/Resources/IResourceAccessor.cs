using NeoVeldrid;
using Pathetic.Renderer.Graph;

namespace Pathetic.Renderer.Resources;

public interface IResourceAccessor
{
    BindableResource GetResource(string name, RenderPass? owner = null);

    T GetResource<T>(string name, RenderPass? owner = null) where T : class, BindableResource
    {
        return GetResource(name, owner) as T ?? throw new InvalidOperationException($"{name} is not a {typeof(T)}");
    }
}

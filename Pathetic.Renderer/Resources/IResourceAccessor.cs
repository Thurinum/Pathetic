using NeoVeldrid;
using Pathetic.Renderer.Graph;

namespace Pathetic.Renderer.Resources;

public interface IResourceAccessor
{
    BindableResource Get(string name, RenderPass? owner = null);

    T Get<T>(string name, RenderPass? owner = null) where T : class, BindableResource
    {
        return Get(name, owner) as T ?? throw new NullReferenceException($"{name} is not a {typeof(T)}");
    } 
    
    Texture GetTexture(string name, RenderPass? owner = null) => Get<Texture>(name, owner);
    DeviceBuffer GetBuffer(string name, RenderPass? owner = null) => Get<DeviceBuffer>(name, owner);
}
using Pathetic.Renderer.Graph;

namespace Pathetic.Renderer.Resources;

public interface IResourceMetadata
{
    uint GetElementCount(string name, RenderPass? owner = null);
}

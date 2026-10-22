using Pathetic.Renderer.Graph;

namespace Pathetic.Renderer.Resources;

public interface IResourceMetadata
{
    SceneChange SceneRevision { get; }
    uint GetElementCount(string name, RenderPass? owner = null);
}

namespace Pathetic.Renderer.Resources;

public interface IResourceProvider
{
    IEnumerable<IResourceSpec> Resources { get; }
}
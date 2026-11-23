namespace Pathetic.Renderer.Graph;

public sealed record RenderGraphSpec
{
    public IReadOnlyList<Type> PassTypes { get; }
    public string? PresentTexture { get; }
    
    internal RenderGraphSpec(IReadOnlyList<Type> passTypes, string? presentTexture)
    {
        PassTypes = passTypes;
        PresentTexture = presentTexture;
    }
}

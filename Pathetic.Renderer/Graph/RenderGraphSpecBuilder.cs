namespace Pathetic.Renderer.Graph;

public sealed class RenderGraphSpecBuilder
{
    private readonly List<Type> _passTypes = [];
    private string? _presentTexture;

    public RenderGraphSpecBuilder AddPass<T>() where T : RenderPass
    {
        if (typeof(T) == typeof(RenderPass))
            throw new InvalidOperationException("You must subclass RenderPass to create your own.");

        _passTypes.Add(typeof(T));
        return this;
    }

    public RenderGraphSpecBuilder Present(string textureName)
    {
        _presentTexture = textureName;
        return this;
    }

    public RenderGraphSpec Build() => new(_passTypes.ToArray(), _presentTexture);
}

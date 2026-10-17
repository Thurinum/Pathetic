namespace Pathetic.Renderer.Graph;

public sealed class SceneRevision
{
    public uint Value { get; private set; }

    public void Invalidate() => Value++;
}

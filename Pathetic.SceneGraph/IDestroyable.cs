namespace Pathetic.SceneGraph;

public interface IDestroyable
{
    public bool IsPendingDestroy { get; }
    public void Destroy();
}
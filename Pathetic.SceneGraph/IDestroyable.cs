namespace Pathetic.SceneGraph;

public interface IDestroyable
{
    internal bool IsDestroyed { get; }
    internal bool IsPendingDestroy { get; set; }
    internal void Destroy();
}
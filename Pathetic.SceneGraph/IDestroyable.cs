namespace Pathetic.SceneGraph;

internal interface IDestroyable
{
    bool IsPendingDestroy { get; }
    void Destroy();
}
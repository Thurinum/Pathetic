using Pathetic.SceneGraph.Component;

namespace Pathetic.SceneGraph.Scene;

public sealed class Scene(ComponentFactory componentFactory)
{
    public List<SceneObject> Objects { get; } = [];
    
    internal ComponentFactory ComponentFactory { get; } = componentFactory;
    internal List<IDestroyable> PendingDestroy { get; } = [];
    
    public void FlushDestroyed()
    {
        if (PendingDestroy.Count == 0)
            return;

        var batch = PendingDestroy.ToArray();
        PendingDestroy.Clear();

        foreach (var destroyable in batch)
            destroyable.Destroy();
    }
}

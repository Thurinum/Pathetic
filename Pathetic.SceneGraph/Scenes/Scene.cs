using Pathetic.SceneGraph.Components;

namespace Pathetic.SceneGraph.Scenes;

public sealed class Scene(ComponentFactory componentFactory)
{
    public List<SceneObject> Objects { get; } = [];
    internal ComponentFactory ComponentFactory { get; } = componentFactory;
    internal Queue<Component> PendingSetup { get; } = new();
    internal Queue<IDestroyable> PendingDestroy { get; } = new();

    internal void MarkForSetup(Component component)
    {
        PendingSetup.Enqueue(component);
    }
    
    internal void MarkForDestroy(IDestroyable destroyable)
    {
        if (destroyable.IsDestroyed || destroyable.IsPendingDestroy)
            return;

        destroyable.IsPendingDestroy = true;
        
        PendingDestroy.Enqueue(destroyable);
    }
}

using Pathetic.SceneGraph.Components;

namespace Pathetic.SceneGraph.Scenes;

public sealed class Scene(ComponentFactory componentFactory)
{
    public List<SceneObject> Objects { get; } = [];
    internal ComponentFactory ComponentFactory { get; } = componentFactory;
    internal List<Component> PendingSetup { get; } = [];
    internal List<IDestroyable> PendingDestroy { get; } = [];

    internal void MarkForSetup(Component component)
    {
        PendingSetup.Add(component);
    }
    
    internal void MarkForDestroy(IDestroyable destroyable)
    {
        if (destroyable.IsDestroyed || destroyable.IsPendingDestroy)
            return;

        destroyable.IsPendingDestroy = true;
        
        PendingDestroy.Add(destroyable);
    }
}

using Pathetic.SceneGraph.Components;

namespace Pathetic.SceneGraph.Scenes;

public sealed class Scene(ComponentFactory componentFactory)
{
    public List<SceneObject> Objects { get; } = [];
    
    internal ComponentFactory ComponentFactory { get; } = componentFactory;
    internal List<Component> PendingSetup { get; } = [];
    internal List<IDestroyable> PendingDestroy { get; } = [];

    public void FlushSetup()
    {
        if (PendingSetup.Count == 0)
            return;
        
        Component[] batch = PendingSetup.ToArray();
        PendingSetup.Clear();
        
        foreach (var component in batch)
        {
            if (component is { IsDestroyed: false, IsPendingDestroy: false })
            {
                ((IComponentLifecycle)component).Setup();
            }
        }
        
        foreach (var component in batch)
        {
            if (component is { IsDestroyed: false, IsPendingDestroy: false })
            {
                ((IComponentLifecycle)component).LateSetup();
            }
        }
    }
    
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

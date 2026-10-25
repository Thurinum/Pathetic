using Pathetic.SceneGraph.Components;

namespace Pathetic.SceneGraph.Scenes;

public sealed partial class Scene(ComponentFactory componentFactory)
{
    public List<SceneObject> Objects { get; } = [];
    internal ComponentFactory ComponentFactory { get; } = componentFactory;
    private List<Component> PendingSetup { get; } = [];
    private List<IDestroyable> PendingDestroy { get; } = [];

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
        while (PendingDestroy.Count > 0)
        {
            var batch = PendingDestroy.ToArray();
            PendingDestroy.Clear();

            foreach (var destroyable in batch)
            {
                try
                {
                    destroyable.Destroy();
                }
                catch (Exception)
                {
                    // todo: log 
                }
            }
        }
    }
}

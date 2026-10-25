using Microsoft.Extensions.Logging;
using Pathetic.SceneGraph.Components;

namespace Pathetic.SceneGraph.Scenes;

public sealed class SceneManager(ComponentFactory componentFactory, ILogger<SceneManager> logger)
{
    public Scene CreateScene()
    {
        return new Scene(componentFactory);
    }
    
    public SceneObject CreateObject(Scene scene, string name)
    {
        var obj = new SceneObject
        {
            Owner = scene,
            Name = name
        };
        scene.Objects.Add(obj);
        obj.Transform = obj.AddComponent<Transform>();
        return obj;
    }

    public void DestroyLater(Scene scene, IDestroyable thing)
    {
        scene.MarkForDestroy(thing);
    }
    
    public void FlushSetup(Scene scene)
    {
        if (scene.PendingSetup.Count == 0)
            return;
        
        Component[] batch = scene.PendingSetup.ToArray();
        scene.PendingSetup.Clear();
        
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
    
    public void FlushDestroyed(Scene scene)
    {
        while (scene.PendingDestroy.TryDequeue(out IDestroyable? destroyable))
        {
            try
            {
                destroyable.Destroy();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during destruction: {EMessage}.", e.Message);
            }
        }
    }
}
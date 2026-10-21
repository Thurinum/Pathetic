using Pathetic.SceneGraph.Components;

namespace Pathetic.SceneGraph.Scene;

public sealed class SceneManager(ComponentFactory componentFactory)
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

    public void DestroyObject(Scene scene, SceneObject obj)
    {
        if (obj.IsPendingDestroy)
            return;

        obj.IsPendingDestroy = true;
        scene.PendingDestroy.Add(obj);
    }
}
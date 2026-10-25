using Pathetic.SceneGraph.Components;

namespace Pathetic.SceneGraph.Scenes;

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

    public void DestroyLater(Scene scene, IDestroyable thing)
    {
        scene.MarkForDestroy(thing);
    }
}
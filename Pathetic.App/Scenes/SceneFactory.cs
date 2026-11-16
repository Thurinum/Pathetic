using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Scenes;

namespace Pathetic.App.Scenes;

public sealed class SceneFactory(SceneManager sceneManager, IScene definition)
{
    public Scene Create()
    {
        Scene scene = sceneManager.CreateScene();

        var builder = new SceneBuilder(sceneManager, scene);
        definition.Build(builder);
        builder.AddCamera(definition.Camera);

        return scene;
    }
}

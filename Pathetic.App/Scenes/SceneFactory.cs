using Pathetic.App.Assets;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Scenes;

namespace Pathetic.App.Scenes;

public sealed class SceneFactory(SceneManager sceneManager, ObjLoader objLoader, IEnumerable<ISceneDefinition> definitions)
{
    public IReadOnlyList<ISceneDefinition> Definitions { get; } = [.. definitions];

    public IReadOnlyList<SceneObject> Populate(Scene scene, ISceneDefinition definition)
    {
        var builder = new SceneBuilder(sceneManager, scene, objLoader);
        definition.Build(builder);
        builder.AddCamera(definition.Camera);

        return builder.Created;
    }
}

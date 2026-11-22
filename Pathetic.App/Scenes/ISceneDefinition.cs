namespace Pathetic.App.Scenes;

public interface ISceneDefinition
{
    OrbitCameraDefinition Camera { get; }

    void Build(SceneBuilder builder);
}

namespace Pathetic.App.Scenes;

public interface IScene
{
    CameraPose Camera { get; }

    void Build(SceneBuilder builder);
}

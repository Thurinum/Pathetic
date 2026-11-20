using System.Numerics;
using static Pathetic.App.Scenes.SceneBuilder;

namespace Pathetic.App.Scenes.Catalog;

public sealed class DefaultScene : IScene
{
    public CameraPose Camera { get; } = new(new Vector3(2, 0, 0), Distance: 5);

    public void Build(SceneBuilder builder)
    {
        builder.AddSphere(new(2.0f, -100.5f, 0.0f), 100.0f, Rgb(0.6f, 0.6f, 0.6f));
        builder.AddSphere(new(2.0f, 0.2f, 0.0f), 0.7f, Rgb(0.9f, 0.6f, 0.2f));
        builder.AddSphere(new(1.3f, -0.3f, 0.9f), 0.2f, Rgb(0.2f, 0.6f, 0.9f));
        builder.AddSphere(new(-0.2f, 1.0f, 0.0f), 0.7f, Rgb(0.0f, 0.0f, 0.0f), Rgb(1.0f, 0.8f, 0.6f), 15.0f);
        builder.AddRoom();
    }
}

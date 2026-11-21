using Pathetic.Renderer.Utils;
using System.Numerics;
using Microsoft.Extensions.Options;
using NeoVeldrid;

namespace Pathetic.App.Scenes.Catalog;

public sealed class DefaultScene(IOptions<SceneOptions> options) : ISceneDefinition
{
    private const float FloorSize = 500f;
    private const float ColorMin = 64;
    private const float ColorMax = 256;

    public OrbitCameraDefinition Camera { get; } =
        new(new Vector3(2, 0, 0), Distance: 11.4f, AzimuthDegrees: -11.3f, ElevationDegrees: 26f);

    public void Build(SceneBuilder builder)
    {
        var random = new Random(options.Value.Seed);
        RgbaByte RandomColor() => new(Channel(random), Channel(random), Channel(random), 255);

        builder.AddPlane(new(0, -0.5f, 0), Quaternion.FromEuler(new(-90, 0, 0)), FloorSize, RgbaByte.Rgb(0.6f, 0.6f, 0.6f));

        builder.AddSphere(new(2.0f, -0.2f, 0.0f), 0.7f, RandomColor());
        builder.AddSphere(new(-1.3f, -0.3f, 0.9f), 0.2f, RandomColor());
        builder.AddSphere(new(-0.2f, 1.0f, 0.0f), 0.7f, RgbaByte.Rgb(0, 0, 0), RgbaByte.Rgb(1.0f, 0.8f, 0.6f), 15.0f);

        builder.AddModel("cube.obj", new(0.5f, 0.0f, 1.5f), 0.5f, albedo: RandomColor());
        builder.AddModel("teapot.obj", new(-2.2f, -0.5f, 3.2f), 1f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, -0.35f), RandomColor());
        builder.AddModel("cow.obj", new(2.2f, 1.3f, -3.2f), 0.5f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, -0.35f), RandomColor());
        builder.AddModel("cube.obj", new(1.4f, -0.35f, 1.0f), 0.15f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.6f), RandomColor());
    }

    private static byte Channel(Random random) => (byte)random.Next((int)ColorMin, (int)ColorMax);
}

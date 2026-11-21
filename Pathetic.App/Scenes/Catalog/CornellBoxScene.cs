using System.Numerics;
using NeoVeldrid;

namespace Pathetic.App.Scenes.Catalog;

public sealed class CornellBoxScene : ISceneDefinition
{
    private const float WallRadius = 500f;
    private const float HalfWidth = 5f;
    private const float Height = 10f;
    private const float HalfDepth = 10f;
    private const float LightStrength = 12f;

    public OrbitCameraDefinition Camera { get; } = new(new Vector3(0, Height / 2, 0), Distance: HalfDepth - 0.5f);

    public void Build(SceneBuilder builder)
    {
        RgbaByte white = RgbaByte.Rgb(0.73f, 0.73f, 0.73f);

        builder.AddSphere(new(-HalfWidth - WallRadius, Height / 2, 0), WallRadius, RgbaByte.Rgb(0.65f, 0.05f, 0.05f));
        builder.AddSphere(new(HalfWidth + WallRadius, Height / 2, 0), WallRadius, RgbaByte.Rgb(0.12f, 0.45f, 0.15f));
        builder.AddSphere(new(0, Height / 2, -HalfDepth - WallRadius), WallRadius, white);
        builder.AddSphere(new(0, -WallRadius, 0), WallRadius, white);
        builder.AddSphere(new(0, Height + WallRadius, 0), WallRadius, white);
        builder.AddSphere(new(0, Height / 2, HalfDepth + WallRadius), WallRadius, RgbaByte.Rgb(0, 0, 0));

        builder.AddSphere(new(0, Height - 0.5f, 0), 1.5f, RgbaByte.Rgb(0, 0, 0), RgbaByte.Rgb(1.0f, 0.9f, 0.7f), LightStrength);
        builder.AddSphere(new(-2.5f, 2.5f, -3), 2.5f, white);
        builder.AddSphere(new(2.5f, 1.8f, 1), 1.8f, white);
    }
}

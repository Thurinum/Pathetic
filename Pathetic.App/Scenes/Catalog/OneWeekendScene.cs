using NeoVeldrid;
using System.Numerics;
using Microsoft.Extensions.Options;

namespace Pathetic.App.Scenes.Catalog;

public sealed class OneWeekendScene(IOptions<SceneOptions> options) : ISceneDefinition
{
    private const int GridHalfExtent = 11;
    private const float SmallRadius = 0.2f;
    private const float GroundRadius = 1000f;
    private const float CellJitter = 0.9f;
    private const float BigSphereClearance = 0.9f;
    private const float DiffuseShare = 0.8f;

    private static readonly Vector3 ClearancePoint = new(4, SmallRadius, 0);

    public OrbitCameraDefinition Camera { get; } = new(new Vector3(0, 0.5f, 0), Distance: 13.4f, AzimuthDegrees: 77, ElevationDegrees: 6.4f);

    public void Build(SceneBuilder builder)
    {
        builder.AddSphere(new(0, -GroundRadius, 0), GroundRadius, RgbaByte.Rgb(0.5f, 0.5f, 0.5f));
        builder.AddSphere(new(0, 1, 0), 1, RgbaByte.Rgb(0.8f, 0.8f, 0.9f));
        builder.AddSphere(new(-4, 1, 0), 1, RgbaByte.Rgb(0.4f, 0.2f, 0.1f));
        builder.AddSphere(new(4, 1, 0), 1, RgbaByte.Rgb(0.7f, 0.6f, 0.5f));

        var random = new Random(options.Value.Seed);
        for (int a = -GridHalfExtent; a < GridHalfExtent; a++)
        {
            for (int b = -GridHalfExtent; b < GridHalfExtent; b++)
            {
                var center = new Vector3(a + CellJitter * random.NextSingle(), SmallRadius, b + CellJitter * random.NextSingle());
                Vector3 color = random.NextSingle() < DiffuseShare ? RandomColor(random) * RandomColor(random) : LightColor(random);

                if (Vector3.Distance(center, ClearancePoint) > BigSphereClearance)
                    builder.AddSphere(center, SmallRadius, RgbaByte.Rgb(color.X, color.Y, color.Z));
            }
        }
    }

    private static Vector3 RandomColor(Random random) => new(random.NextSingle(), random.NextSingle(), random.NextSingle());

    private static Vector3 LightColor(Random random) => Vector3.One * 0.5f + RandomColor(random) * 0.5f;
}

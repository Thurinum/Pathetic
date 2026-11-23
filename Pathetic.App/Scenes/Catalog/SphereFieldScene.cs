using NeoVeldrid;
using System.Numerics;
using Microsoft.Extensions.Options;

namespace Pathetic.App.Scenes.Catalog;

public sealed class SphereFieldScene(IOptions<SceneOptions> options) : ISceneDefinition
{
    private const float MinRadius = 0.08f;
    private const float RadiusRange = 0.15f;
    private const float LightProbability = 0.05f;
    private const float LightStrength = 8.0f;

    private static readonly Vector3 FieldMin = new(-10.0f, -0.5f, -10.0f);
    private static readonly Vector3 FieldSize = new(20.0f, 3.0f, 20.0f);

    public OrbitCameraDefinition Camera { get; } = new(new Vector3(0, 1, 0), Distance: 22, ElevationDegrees: 25);

    public void Build(SceneBuilder builder)
    {
        var random = new Random(options.Value.Seed);
        for (int i = 0; i < options.Value.SphereCount; i++)
        {
            Vector3 position = FieldMin + new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()) * FieldSize;
            float radius = MinRadius + random.NextSingle() * RadiusRange;
            var albedo = RgbaByte.Rgb(random.NextSingle(), random.NextSingle(), random.NextSingle());

            if (random.NextSingle() < LightProbability)
                builder.AddSphere(position, radius, albedo, RgbaByte.Rgb(1.0f, 0.9f, 0.7f), LightStrength);
            else
                builder.AddSphere(position, radius, albedo);
        }
    }
}

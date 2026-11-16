using System.Numerics;
using NeoVeldrid;
using static Pathetic.App.Scenes.SceneBuilder;

namespace Pathetic.App.Scenes.Catalog;

public sealed class SphereflakeScene : IScene
{
    private const int MaxDepth = 4;
    private const float ChildScale = 1f / 3f;
    private const int RingChildren = 6;
    private const int CapChildren = 3;
    private const float CapElevation = MathF.PI / 3;
    private const float GroundRadius = 1000f;
    private const float RootHeight = 1.5f;

    private static readonly RgbaByte[] DepthColors =
    [
        Rgb(0.9f, 0.9f, 0.9f), Rgb(0.9f, 0.3f, 0.3f), Rgb(0.9f, 0.7f, 0.2f), Rgb(0.3f, 0.8f, 0.4f), Rgb(0.3f, 0.5f, 0.9f),
    ];

    public CameraPose Camera { get; } = new(new Vector3(0, RootHeight, 0), Distance: 9, ElevationDegrees: 15);

    public void Build(SceneBuilder builder)
    {
        builder.AddSphere(new(0, -GroundRadius, 0), GroundRadius, Rgb(0.5f, 0.5f, 0.5f));
        AddFlake(builder, new Vector3(0, RootHeight, 0), 1f, Vector3.UnitY, depth: 0);
    }

    private static void AddFlake(SceneBuilder builder, Vector3 center, float radius, Vector3 axis, int depth)
    {
        builder.AddSphere(center, radius, DepthColors[depth]);

        if (depth == MaxDepth)
            return;

        float childRadius = radius * ChildScale;
        foreach (Vector3 direction in ChildDirections(axis))
            AddFlake(builder, center + direction * (radius + childRadius), childRadius, direction, depth + 1);
    }

    private static IEnumerable<Vector3> ChildDirections(Vector3 axis)
    {
        Vector3 reference = MathF.Abs(axis.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX;
        Vector3 u = Vector3.Normalize(Vector3.Cross(axis, reference));
        Vector3 v = Vector3.Cross(axis, u);

        Vector3 Around(float angle, float elevation) =>
            MathF.Cos(elevation) * (MathF.Cos(angle) * u + MathF.Sin(angle) * v) + MathF.Sin(elevation) * axis;

        IEnumerable<Vector3> ring = Enumerable.Range(0, RingChildren).Select(i => Around(i * MathF.Tau / RingChildren, 0));
        IEnumerable<Vector3> cap = Enumerable.Range(0, CapChildren).Select(i => Around(i * MathF.Tau / CapChildren, CapElevation));
        return ring.Concat(cap);
    }
}

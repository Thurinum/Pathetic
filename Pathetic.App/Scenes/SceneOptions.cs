using System.ComponentModel.DataAnnotations;
using Pathetic.Renderer;

namespace Pathetic.App.Scenes;

public sealed record SceneOptions : IConfig
{
    private const int MaxSphereCount = 1_000_000;

    public static string Section => "Scene";

    [Range(0, MaxSphereCount)]
    public int SphereCount { get; init; } = 2000;

    public int Seed { get; init; } = 1234;
}

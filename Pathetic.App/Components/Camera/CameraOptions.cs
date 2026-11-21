using System.ComponentModel.DataAnnotations;
using Pathetic.Renderer;

namespace Pathetic.App.Components.Camera;

public sealed record CameraOptions : IConfig
{
    public static string Section => "Camera";

    [Range(1.0, 179.0)]
    public float FovDegrees { get; init; } = 80.0f;

    [Range(0.0, 1000.0)]
    public float FocusDistance { get; init; } = 5.0f;

    [Range(0.0, 10.0)]
    public float ApertureRadius { get; init; } = 0.15f;
}

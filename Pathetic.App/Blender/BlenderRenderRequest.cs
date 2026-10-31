using System.Text.Json;

namespace Pathetic.App.Blender;

/// <summary>
/// Scene description sent by the Blender add-on. Coordinates are already in Pathetic's
/// right-handed, Y-up space. Colors are linear bytes (0..255), matching RgbaByte.
/// </summary>
public sealed class BlenderRenderRequest
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public uint Width { get; init; } = 640;
    public uint Height { get; init; } = 480;
    public uint Samples { get; init; } = 64;
    public uint SamplesPerPixel { get; init; } = 8;
    public uint MaxBounces { get; init; } = 8;
    public string? Tonemapper { get; init; }
    public float Exposure { get; init; }

    public CameraDto Camera { get; init; } = new();
    public List<SphereDto> Spheres { get; init; } = [];
    public List<PlaneDto> Planes { get; init; } = [];
    public List<TriangleDto> Triangles { get; init; } = [];

    public static BlenderRenderRequest Load(string path)
    {
        string json = File.ReadAllText(path);
        BlenderRenderRequest? request = JsonSerializer.Deserialize<BlenderRenderRequest>(
            json,
            SerializerOptions);

        if (request is null)
            throw new InvalidOperationException($"Could not deserialize render request '{path}'.");

        return request;
    }
}

public sealed class CameraDto
{
    public List<float> Origin { get; init; } = [0, 0, 0];
    public List<float> Right { get; init; } = [1, 0, 0];
    public List<float> Up { get; init; } = [0, 1, 0];
    public List<float> Forward { get; init; } = [0, 0, -1];
    public float TanHalfFov { get; init; } = 0.5f;
    public float FocusDistance { get; init; } = 5.0f;
    public float ApertureRadius { get; init; }
}

public sealed class SphereDto
{
    public List<float> Center { get; init; } = [0, 0, 0];
    public float Radius { get; init; } = 1.0f;
    public List<byte> Albedo { get; init; } = [128, 128, 128, 255];
    public List<byte> Emission { get; init; } = [0, 0, 0, 255];
}

public sealed class PlaneDto
{
    public List<float> Point { get; init; } = [0, 0, 0];
    public List<float> ExtentX { get; init; } = [1, 0, 0];
    public List<float> ExtentY { get; init; } = [0, 1, 0];
}

public sealed class TriangleDto
{
    public List<float> V0 { get; init; } = [0, 0, 0];
    public List<float> V1 { get; init; } = [1, 0, 0];
    public List<float> V2 { get; init; } = [0, 1, 0];
    public List<byte> Albedo { get; init; } = [128, 128, 128, 255];
    public List<byte> Emission { get; init; } = [0, 0, 0, 255];
}

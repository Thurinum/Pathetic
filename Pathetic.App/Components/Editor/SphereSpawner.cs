using System.Numerics;
using ImGuiNET;
using NeoVeldrid;
using Pathetic.App.Components.Camera;
using Pathetic.Renderer.Graph;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components.Editor;

// Middle-click drops a fresh dynamic sphere (random radius and colour) where the cursor ray meets the world.
public sealed class SphereSpawner(CameraProvider camera, SceneRevision revision) : Component
{
    private const float MinRadius = 0.12f;
    private const float MaxRadius = 0.45f;
    private const float FallbackDistance = 4f;
    private const float MaxSpawnDistance = 30f;

    public override void OnGUI()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        if (io.WantCaptureMouse || !ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
            return;

        if (!EditorCamera.TryBuildRay(camera.Data, ImGui.GetMousePos(), io.DisplaySize, out ViewRay ray))
            return;

        Vector3 position = ResolveSpawnPosition(ray);
        float radius = MinRadius + (Random.Shared.NextSingle() * (MaxRadius - MinRadius));
        RgbaByte albedo = RandomAlbedo();

        revision.Invalidate();
        Root.DeferCreate(() => SceneFactory.AddSphere(Root, position, radius, albedo));
    }

    // Spawn where the cursor ray crosses the world plane y = 0; fall back to a point in front of the camera.
    private static Vector3 ResolveSpawnPosition(in ViewRay ray)
    {
        Vector3 fallback = ray.Origin + (ray.Direction * FallbackDistance);

        if (MathF.Abs(ray.Direction.Y) < 1e-4f)
            return fallback;

        float distance = -ray.Origin.Y / ray.Direction.Y;
        if (distance <= 0f || distance > MaxSpawnDistance)
            return fallback;

        return ray.Origin + (ray.Direction * distance);
    }

    private static RgbaByte RandomAlbedo()
    {
        const float saturation = 0.55f;
        const float value = 0.95f;

        float hue = Random.Shared.NextSingle() * 6f;
        int sector = (int)hue;
        float fraction = hue - sector;

        float p = value * (1f - saturation);
        float q = value * (1f - (saturation * fraction));
        float t = value * (1f - (saturation * (1f - fraction)));

        (float red, float green, float blue) = sector switch
        {
            0 => (value, t, p),
            1 => (q, value, p),
            2 => (p, value, t),
            3 => (p, q, value),
            4 => (t, p, value),
            _ => (value, p, q)
        };

        return new RgbaByte(
            (byte)MathF.Round(red * 255f),
            (byte)MathF.Round(green * 255f),
            (byte)MathF.Round(blue * 255f),
            255);
    }
}

using System.Numerics;
using Pathetic.App.Components.Camera;

namespace Pathetic.App.Components.Editor;

internal readonly record struct ViewRay(Vector3 Origin, Vector3 Direction);

// CPU mirror of the primary-ray / projection math in Camera.slang, used by the editor gizmos.
internal static class EditorCamera
{
    public static bool TryBuildRay(in CameraData camera, Vector2 screenPosition, Vector2 displaySize, out ViewRay ray)
    {
        ray = default;

        if (displaySize.X <= 0f || displaySize.Y <= 0f || camera.Forward.LengthSquared() < 0.5f)
            return false;

        float aspect = displaySize.X / displaySize.Y;
        Vector2 uv = screenPosition / displaySize;
        float viewPlaneX = ((2.0f * uv.X) - 1.0f) * aspect * camera.TanHalfFOV;
        float viewPlaneY = (1.0f - (2.0f * uv.Y)) * camera.TanHalfFOV;

        Vector3 direction = camera.Forward + (viewPlaneX * camera.Right) + (viewPlaneY * camera.Up);
        ray = new ViewRay(camera.Origin, Vector3.Normalize(direction));
        return true;
    }

    public static bool TryProject(in CameraData camera, Vector3 world, Vector2 displaySize, out Vector2 screen)
    {
        screen = default;

        if (displaySize.X <= 0f || displaySize.Y <= 0f || camera.Forward.LengthSquared() < 0.5f)
            return false;

        Vector3 toPoint = world - camera.Origin;
        float depth = Vector3.Dot(toPoint, camera.Forward);
        if (depth <= 1e-4f)
            return false;

        float aspect = displaySize.X / displaySize.Y;
        float ndcX = Vector3.Dot(toPoint, camera.Right) / (depth * camera.TanHalfFOV * aspect);
        float ndcY = Vector3.Dot(toPoint, camera.Up) / (depth * camera.TanHalfFOV);

        screen = new Vector2(
            ((ndcX * 0.5f) + 0.5f) * displaySize.X,
            (1.0f - ((ndcY * 0.5f) + 0.5f)) * displaySize.Y);
        return true;
    }
}

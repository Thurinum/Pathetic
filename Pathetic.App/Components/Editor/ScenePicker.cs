using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components.Editor;

// CPU raycast against the scene, mirroring the intersection math in Primitives.slang.
internal static class ScenePicker
{
    public static bool TryPickSphere(Scene scene, in ViewRay ray, [NotNullWhen(true)] out SceneObject? hit)
    {
        hit = null;
        float nearest = float.PositiveInfinity;

        foreach (SceneObject candidate in scene.Objects)
        {
            if (!candidate.TryGetComponent<SphereComponent>(out SphereComponent? sphere) || sphere is null)
                continue;

            if (!IntersectSphere(ray, candidate.Transform.Position, sphere.Radius, 1e-4f, nearest, out float distance))
                continue;

            nearest = distance;
            hit = candidate;
        }

        return hit is not null;
    }

    private static bool IntersectSphere(in ViewRay ray, Vector3 center, float radius, float tMin, float tMax, out float distance)
    {
        Vector3 offset = ray.Origin - center;
        float a = Vector3.Dot(ray.Direction, ray.Direction);
        float b = 2f * Vector3.Dot(offset, ray.Direction);
        float c = Vector3.Dot(offset, offset) - (radius * radius);
        float discriminant = (b * b) - (4f * a * c);

        if (discriminant < 0f)
        {
            distance = 0f;
            return false;
        }

        float root = MathF.Sqrt(discriminant);
        distance = (-b - root) / (2f * a);
        if (distance <= tMin || distance >= tMax)
            distance = (-b + root) / (2f * a);

        if (distance <= tMin || distance >= tMax)
        {
            distance = 0f;
            return false;
        }

        return true;
    }
}

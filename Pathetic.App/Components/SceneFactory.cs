using System.Numerics;
using NeoVeldrid;
using Pathetic.Physics;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

// Shared runtime construction of scene objects, reused by the initial scene setup and the editor spawners.
internal static class SceneFactory
{
    public static SceneObject AddSphere(Scene scene, Vector3 position, float radius, RgbaByte albedo, RgbaByte emission = default, bool dynamic = true)
    {
        SceneObject obj = SceneManager.CreateObject(scene, "sphere");
        obj.Transform.Position = position;

        SphereComponent sphere = obj.AddComponent<SphereComponent>();
        sphere.Radius = radius;
        sphere.Albedo = albedo;
        sphere.Emission = emission;

        SphereCollider collider = obj.AddComponent<SphereCollider>();
        collider.Radius = radius;

        RigidBody body = obj.AddComponent<RigidBody>();
        body.Kind = dynamic ? BodyKind.Dynamic : BodyKind.Static;
        body.Mass = 1.0f;
        body.Restitution = 0.2f;
        body.Friction = 0.5f;

        return obj;
    }
}

using System.Numerics;
using Pathetic.SceneGraph;

namespace Pathetic.Physics;

// Scene-side description of a body's collision geometry. A RigidBody gathers the Collider components
// on the same SceneObject when it creates its body.
public abstract class Collider : Component
{
    internal abstract Shape BuildShape();
}

public sealed class SphereCollider : Collider
{
    public float Radius { get; set; } = 0.5f;

    internal override Shape BuildShape() => new SphereShape(Radius);
}

public sealed class BoxCollider : Collider
{
    // Full extent along each axis.
    public Vector3 Size { get; set; } = Vector3.One;

    internal override Shape BuildShape() => new BoxShape(Size);
}

public sealed class CapsuleCollider : Collider
{
    public float Radius { get; set; } = 0.5f;
    public float Length { get; set; } = 1f;

    internal override Shape BuildShape() => new CapsuleShape(Radius, Length);
}

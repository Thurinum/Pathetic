using System.Numerics;

namespace Pathetic.Physics;

// Description used to create a body. Plain data: no middleware types leak through here.
public sealed record BodyDesc
{
    public BodyKind Kind { get; init; } = BodyKind.Dynamic;
    public Pose Pose { get; init; } = Pose.Identity;
    public Vector3 LinearVelocity { get; init; }
    public float Mass { get; init; } = 1f;
    public float Friction { get; init; } = 0.3f;
    public float Restitution { get; init; }
    public bool AffectedByGravity { get; init; } = true;
    public IReadOnlyList<Shape> Shapes { get; init; } = [];
}

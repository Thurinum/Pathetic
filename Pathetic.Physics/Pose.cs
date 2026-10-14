using System.Numerics;

namespace Pathetic.Physics;

// World-space placement of a body: position plus orientation.
public readonly record struct Pose(Vector3 Position, Quaternion Rotation)
{
    public static Pose Identity => new(Vector3.Zero, Quaternion.Identity);
}

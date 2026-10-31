using System.Numerics;

namespace Pathetic.App.Bvh;

public readonly record struct AxisAlignedBoundingBox(Vector3 Min, Vector3 Max)
{
    public static AxisAlignedBoundingBox Empty =>
        new(new Vector3(float.PositiveInfinity), new Vector3(float.NegativeInfinity));

    public Vector3 Centroid => (Min + Max) * 0.5f;

    public AxisAlignedBoundingBox Include(AxisAlignedBoundingBox other) =>
        new(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));

    public AxisAlignedBoundingBox Include(Vector3 point) =>
        new(Vector3.Min(Min, point), Vector3.Max(Max, point));
}

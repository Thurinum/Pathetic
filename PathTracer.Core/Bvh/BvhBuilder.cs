namespace PathTracerCore.Bvh;

using System.Numerics;

public sealed class BvhBuilder
{
    private const int MaxLeafSize = 4;

    public BvhNode Build(List<BvhPrimitiveRef> primitives)
    {
        if (primitives.Count == 0)
            throw new ArgumentException("Cannot build a BVH with no primitives.");

        return BuildRecursive(primitives);
    }

    private static BvhNode BuildRecursive(List<BvhPrimitiveRef> primitives)
    {
        var bounds = primitives
            .Select(p => p.Bounds)
            .Aggregate(
                AxisAlignedBoundingBox.Empty,
                (current, next) => current.Include(next));

        if (primitives.Count <= MaxLeafSize)
        {
            return new BvhNode
            {
                Bounds = bounds,
                Primitives = primitives
            };
        }

        var centroidBounds = primitives
            .Select(p => p.Bounds.Centroid)
            .Aggregate(
                AxisAlignedBoundingBox.Empty,
                (current, next) => current.Include(next));

        var extent = centroidBounds.Max - centroidBounds.Min;
        var axis = LargestAxis(extent);

        primitives.Sort((a, b) =>
            a.Bounds.Centroid[axis].CompareTo(b.Bounds.Centroid[axis]));

        var middle = primitives.Count / 2;

        if (NumberOfLevelsBelow(primitives.Count) > 3)
        {
            BvhNode leftNode = null;
            BvhNode rightNode = null;

            Parallel.Invoke(
                () => leftNode = BuildRecursive(primitives.GetRange(0, middle)),
                () => rightNode = BuildRecursive(primitives.GetRange(middle, primitives.Count - middle))
            );

            return new BvhNode
            {
                Bounds = bounds,
                Left = leftNode,
                Right = rightNode
            };
        } else
        {
            return new BvhNode
            {
                Bounds = bounds,
                Left = BuildRecursive(primitives.GetRange(0, middle)),
                Right = BuildRecursive(primitives.GetRange(middle, primitives.Count - middle)),
            };
        }
    }

    private static int LargestAxis(Vector3 extent)
    {
        if (extent.X >= extent.Y && extent.X >= extent.Z)
            return 0;

        return extent.Y >= extent.Z ? 1 : 2;
    }

    private static int NumberOfLevelsBelow(int numPrimitives)
    {
        return (int)Math.Floor(Math.Log2((float)numPrimitives / MaxLeafSize));
    }
}
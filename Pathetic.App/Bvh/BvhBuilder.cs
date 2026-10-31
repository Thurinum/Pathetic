using System.Numerics;

namespace Pathetic.App.Bvh;

/// <summary>
/// Median-split BVH builder. This is intentionally straightforward rather than optimal:
/// the renderer only needs a correct acceleration structure for arbitrary triangle meshes.
/// </summary>
public sealed class BvhBuilder
{
    private const int MaxLeafSize = 4;
    private const int ParallelDepth = 3;

    public BvhNode Build(List<BvhPrimitiveRef> primitives)
    {
        if (primitives.Count == 0)
            throw new ArgumentException("Cannot build a BVH with no primitives.", nameof(primitives));

        return BuildRecursive(primitives, 0);
    }

    private static BvhNode BuildRecursive(List<BvhPrimitiveRef> primitives, int depth)
    {
        AxisAlignedBoundingBox bounds = BoundsOf(primitives);

        if (primitives.Count <= MaxLeafSize)
        {
            return new BvhNode
            {
                Bounds = bounds,
                Primitives = primitives,
            };
        }

        AxisAlignedBoundingBox centroidBounds = BoundsOfCentroids(primitives);
        Vector3 extent = centroidBounds.Max - centroidBounds.Min;
        int axis = LargestAxis(extent);

        primitives.Sort((a, b) => a.Bounds.Centroid[axis].CompareTo(b.Bounds.Centroid[axis]));

        int middle = primitives.Count / 2;
        List<BvhPrimitiveRef> leftPrimitives = primitives.GetRange(0, middle);
        List<BvhPrimitiveRef> rightPrimitives = primitives.GetRange(middle, primitives.Count - middle);

        if (depth < ParallelDepth && primitives.Count >= 256)
        {
            BvhNode? left = null;
            BvhNode? right = null;

            Parallel.Invoke(
                () => left = BuildRecursive(leftPrimitives, depth + 1),
                () => right = BuildRecursive(rightPrimitives, depth + 1));

            return new BvhNode
            {
                Bounds = bounds,
                Left = left,
                Right = right,
            };
        }

        return new BvhNode
        {
            Bounds = bounds,
            Left = BuildRecursive(leftPrimitives, depth + 1),
            Right = BuildRecursive(rightPrimitives, depth + 1),
        };
    }

    private static AxisAlignedBoundingBox BoundsOf(List<BvhPrimitiveRef> primitives)
    {
        AxisAlignedBoundingBox bounds = AxisAlignedBoundingBox.Empty;

        foreach (BvhPrimitiveRef primitive in primitives)
            bounds = bounds.Include(primitive.Bounds);

        return bounds;
    }

    private static AxisAlignedBoundingBox BoundsOfCentroids(List<BvhPrimitiveRef> primitives)
    {
        AxisAlignedBoundingBox bounds = AxisAlignedBoundingBox.Empty;

        foreach (BvhPrimitiveRef primitive in primitives)
            bounds = bounds.Include(primitive.Bounds.Centroid);

        return bounds;
    }

    private static int LargestAxis(Vector3 extent)
    {
        if (extent.X >= extent.Y && extent.X >= extent.Z)
            return 0;

        return extent.Y >= extent.Z ? 1 : 2;
    }
}

namespace Pathetic.Renderer.Bvh;

using System.Numerics;

struct Bucket
{
    public int Count;
    public AxisAlignedBoundingBox Bounds;
}

public sealed class BvhBuilder
{
    private const int MaxLeafSize = 4;

    private const int BucketCount = 8;
    private const float IntersectionCost = 1;
    private const float TraversalCost = 1;

    public static BvhNode Build(List<BvhPrimitiveRef> primitives)
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

        if (primitives.Count <= 1)
            return BuildLeaf(bounds, primitives);

        var centroidBounds = primitives
            .Select(p => p.Bounds.Centroid)
            .Aggregate(
                AxisAlignedBoundingBox.Empty,
                (current, next) => current.Include(next));

        var sahSplit = FindSahSplit(primitives, bounds, centroidBounds);

        if (sahSplit is { } split && (split.bestCost < primitives.Count * IntersectionCost || primitives.Count > MaxLeafSize))
        {
            return BuildSahBranch(primitives, bounds, centroidBounds, split.bestAxis, split.bestBucket);
        }

        if (primitives.Count <= MaxLeafSize)
            return BuildLeaf(bounds, primitives);

        return BuildMedianSplitBranch(bounds, centroidBounds, primitives);
    }

    private static BvhNode BuildLeaf(AxisAlignedBoundingBox bounds, List<BvhPrimitiveRef> primitives) =>
        new()
        {
            Bounds = bounds,
            Primitives = primitives
        };

    private static BvhNode BuildSahBranch(
        List<BvhPrimitiveRef> primitives,
        AxisAlignedBoundingBox bounds,
        AxisAlignedBoundingBox centroidBounds,
        int axis,
        int bucket)
    {
        var left = new List<BvhPrimitiveRef>();
        var right = new List<BvhPrimitiveRef>();
        var axisMin = centroidBounds.Min[axis];
        var axisExtent = centroidBounds.Max[axis] - axisMin;

        foreach (var primitive in primitives)
        {
            if (BucketIndex(primitive.Bounds.Centroid[axis], axisMin, axisExtent) <= bucket)
                left.Add(primitive);
            else
                right.Add(primitive);
        }

        return BuildBranch(bounds, left, right);
    }

    private static BvhNode BuildMedianSplitBranch(
        AxisAlignedBoundingBox bounds,
        AxisAlignedBoundingBox centroidBounds,
        List<BvhPrimitiveRef> primitives)
    {
        var axis = LargestAxis(centroidBounds.Max - centroidBounds.Min);
        primitives.Sort((a, b) =>
            a.Bounds.Centroid[axis].CompareTo(b.Bounds.Centroid[axis]));

        var middle = primitives.Count / 2;
        return BuildBranch(
            bounds,
            primitives.GetRange(0, middle),
            primitives.GetRange(middle, primitives.Count - middle));
    }

    private static BvhNode BuildBranch(
        AxisAlignedBoundingBox bounds,
        List<BvhPrimitiveRef> left,
        List<BvhPrimitiveRef> right)
    {
        BvhNode? leftNode = null;
        BvhNode? rightNode = null;
        var levelsBelow = NumberOfLevelsBelow(left.Count + right.Count);

        if (levelsBelow > 3)
        {
            Parallel.Invoke(
                () => leftNode = BuildRecursive(left),
                () => rightNode = BuildRecursive(right));
        }
        else
        {
            leftNode = BuildRecursive(left);
            rightNode = BuildRecursive(right);
        }

        return new BvhNode
        {
            Bounds = bounds,
            Left = leftNode,
            Right = rightNode
        };
    }

    private static (int bestAxis, int bestBucket, float bestCost)? FindSahSplit(
        List<BvhPrimitiveRef> primitives,
        AxisAlignedBoundingBox bounds,
        AxisAlignedBoundingBox centroidBounds)
    {
        var bestAxis = -1;
        var bestBucket = -1;
        var bestCost = float.PositiveInfinity;
        var parentArea = bounds.SurfaceArea;

        if (parentArea <= 0)
            return null;

        for (var axis = 0; axis < 3; axis++)
        {
            var axisMin = centroidBounds.Min[axis];
            var axisExtent = centroidBounds.Max[axis] - axisMin;

            var buckets = new Bucket[BucketCount];
            for (var i = 0; i < BucketCount; i++)
                buckets[i].Bounds = AxisAlignedBoundingBox.Empty;

            if (axisExtent <= 0)
                continue;

            foreach (var primitive in primitives)
            {
                var bucket = BucketIndex(primitive.Bounds.Centroid[axis], axisMin, axisExtent);
                buckets[bucket].Count++;
                buckets[bucket].Bounds = buckets[bucket].Bounds.Include(primitive.Bounds);
            }

            var leftCounts = new int[BucketCount - 1];
            var rightCounts = new int[BucketCount - 1];
            var leftBounds = new AxisAlignedBoundingBox[BucketCount - 1];
            var rightBounds = new AxisAlignedBoundingBox[BucketCount - 1];

            var leftCount = 0;
            var leftBoundsAccumulator = AxisAlignedBoundingBox.Empty;
            for (var i = 0; i < BucketCount - 1; i++)
            {
                leftCount += buckets[i].Count;
                leftBoundsAccumulator = leftBoundsAccumulator.Include(buckets[i].Bounds);
                leftCounts[i] = leftCount;
                leftBounds[i] = leftBoundsAccumulator;
            }

            var rightCount = 0;
            var rightBoundsAccumulator = AxisAlignedBoundingBox.Empty;
            for (var i = BucketCount - 1; i > 0; i--)
            {
                rightCount += buckets[i].Count;
                rightBoundsAccumulator = rightBoundsAccumulator.Include(buckets[i].Bounds);
                rightCounts[i - 1] = rightCount;
                rightBounds[i - 1] = rightBoundsAccumulator;
            }

            for (var i = 0; i < BucketCount - 1; i++)
            {
                if (leftCounts[i] == 0 || rightCounts[i] == 0)
                    continue;

                var cost = TraversalCost
                    + IntersectionCost * (
                        leftCounts[i] * leftBounds[i].SurfaceArea
                        + rightCounts[i] * rightBounds[i].SurfaceArea)
                    / parentArea;

                if (!(cost < bestCost)) continue;
                bestCost = cost;
                bestAxis = axis;
                bestBucket = i;
            }
        }

        return bestAxis < 0 ? null : (bestAxis, bestBucket, bestCost);
    }

    private static int BucketIndex(float centroid, float axisMin, float axisExtent) =>
        axisExtent > 0
            ? Math.Clamp((int)(((centroid - axisMin) / axisExtent) * BucketCount), 0, BucketCount - 1)
            : 0;

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

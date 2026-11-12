using System.Numerics;

namespace Pathetic.Renderer.Bvh;

public sealed class BvhBuilder
{
    private const int MaxLeafSize = 3;
    private const int BinCount = 16;
    private const int ParallelBuildThreshold = 256;
    private const int MaxSahDepth = 12;

    private struct Bin
    {
        public int Count;
        public AxisAlignedBoundingBox Bounds;

        public void Add(AxisAlignedBoundingBox bounds)
        {
            Count++;
            Bounds = Bounds.Include(bounds);
        }
    }

    public BvhNode Build(List<BvhPrimitiveRef> primitives)
    {
        if (primitives.Count == 0)
            throw new ArgumentException("Cannot build a BVH with no primitives.", nameof(primitives));

        return BuildRecursive(primitives);
    }

    private static BvhNode BuildRecursive(List<BvhPrimitiveRef> primitives, int depth = 0)
    {
        AxisAlignedBoundingBox bounds = ComputeBounds(primitives);
        if (primitives.Count <= MaxLeafSize)
            return MakeLeaf(bounds, primitives);

        AxisAlignedBoundingBox centroidBounds = ComputeCentroidBounds(primitives);
        if (depth >= MaxSahDepth
            || !TryFindSahSplit(primitives, centroidBounds, out int axis, out int splitBin))
            return MakeMedianSplit(primitives, bounds, centroidBounds, depth);

        float centroidMin = centroidBounds.Min[axis];
        float centroidExtent = centroidBounds.Max[axis] - centroidMin;
        List<BvhPrimitiveRef> left = new(primitives.Count);
        List<BvhPrimitiveRef> right = new(primitives.Count);

        foreach (BvhPrimitiveRef primitive in primitives)
        {
            int bin = GetBin(primitive.Bounds.Centroid[axis], centroidMin, centroidExtent);
            (bin <= splitBin ? left : right).Add(primitive);
        }

        // Degenerate or numerically collapsed partitions still need to make progress.
        if (left.Count == 0 || right.Count == 0)
            return MakeMedianSplit(primitives, bounds, centroidBounds, depth);

        BvhNode leftNode = null!;
        BvhNode rightNode = null!;
        if (primitives.Count >= ParallelBuildThreshold)
        {
            Parallel.Invoke(
                () => leftNode = BuildRecursive(left, depth + 1),
                () => rightNode = BuildRecursive(right, depth + 1));
        }
        else
        {
            leftNode = BuildRecursive(left, depth + 1);
            rightNode = BuildRecursive(right, depth + 1);
        }

        return new BvhNode
        {
            Bounds = bounds,
            Left = leftNode,
            Right = rightNode
        };
    }

    private static bool TryFindSahSplit(
        List<BvhPrimitiveRef> primitives,
        AxisAlignedBoundingBox centroidBounds,
        out int bestAxis,
        out int bestSplitBin)
    {
        bestAxis = -1;
        bestSplitBin = -1;
        float bestCost = float.PositiveInfinity;

        for (int axis = 0; axis < 3; axis++)
        {
            float centroidMin = centroidBounds.Min[axis];
            float centroidExtent = centroidBounds.Max[axis] - centroidMin;
            if (!(centroidExtent > 0.0f) || !float.IsFinite(centroidExtent))
                continue;

            Bin[] bins = new Bin[BinCount];
            for (int i = 0; i < bins.Length; i++)
                bins[i].Bounds = AxisAlignedBoundingBox.Empty;

            foreach (BvhPrimitiveRef primitive in primitives)
            {
                int bin = GetBin(primitive.Bounds.Centroid[axis], centroidMin, centroidExtent);
                bins[bin].Add(primitive.Bounds);
            }

            int[] leftCounts = new int[BinCount - 1];
            int[] rightCounts = new int[BinCount - 1];
            AxisAlignedBoundingBox[] leftBounds = new AxisAlignedBoundingBox[BinCount - 1];
            AxisAlignedBoundingBox[] rightBounds = new AxisAlignedBoundingBox[BinCount - 1];

            int count = 0;
            AxisAlignedBoundingBox accumulatedBounds = AxisAlignedBoundingBox.Empty;
            for (int i = 0; i < BinCount - 1; i++)
            {
                count += bins[i].Count;
                if (bins[i].Count > 0)
                    accumulatedBounds = accumulatedBounds.Include(bins[i].Bounds);
                leftCounts[i] = count;
                leftBounds[i] = accumulatedBounds;
            }

            count = 0;
            accumulatedBounds = AxisAlignedBoundingBox.Empty;
            for (int i = BinCount - 1; i > 0; i--)
            {
                count += bins[i].Count;
                if (bins[i].Count > 0)
                    accumulatedBounds = accumulatedBounds.Include(bins[i].Bounds);
                rightCounts[i - 1] = count;
                rightBounds[i - 1] = accumulatedBounds;
            }

            for (int i = 0; i < BinCount - 1; i++)
            {
                if (leftCounts[i] == 0 || rightCounts[i] == 0)
                    continue;

                float cost = leftCounts[i] * leftBounds[i].SurfaceArea
                    + rightCounts[i] * rightBounds[i].SurfaceArea;
                if (float.IsFinite(cost) && cost < bestCost)
                {
                    bestCost = cost;
                    bestAxis = axis;
                    bestSplitBin = i;
                }
            }
        }

        return bestAxis >= 0;
    }

    private static BvhNode MakeMedianSplit(
        List<BvhPrimitiveRef> primitives,
        AxisAlignedBoundingBox bounds,
        AxisAlignedBoundingBox centroidBounds,
        int depth)
    {
        Vector3 extent = centroidBounds.Max - centroidBounds.Min;
        int axis = LargestAxis(extent);
        primitives.Sort((a, b) =>
            a.Bounds.Centroid[axis].CompareTo(b.Bounds.Centroid[axis]));

        int middle = primitives.Count / 2;
        List<BvhPrimitiveRef> left = primitives.GetRange(0, middle);
        List<BvhPrimitiveRef> right = primitives.GetRange(middle, primitives.Count - middle);
        BvhNode leftNode = null!;
        BvhNode rightNode = null!;

        if (primitives.Count >= ParallelBuildThreshold)
        {
            Parallel.Invoke(
                () => leftNode = BuildRecursive(left, depth + 1),
                () => rightNode = BuildRecursive(right, depth + 1));
        }
        else
        {
            leftNode = BuildRecursive(left, depth + 1);
            rightNode = BuildRecursive(right, depth + 1);
        }

        return new BvhNode
        {
            Bounds = bounds,
            Left = leftNode,
            Right = rightNode
        };
    }

    private static BvhNode MakeLeaf(AxisAlignedBoundingBox bounds, List<BvhPrimitiveRef> primitives) =>
        new()
        {
            Bounds = bounds,
            Primitives = primitives
        };

    private static AxisAlignedBoundingBox ComputeBounds(List<BvhPrimitiveRef> primitives)
    {
        AxisAlignedBoundingBox bounds = AxisAlignedBoundingBox.Empty;
        foreach (BvhPrimitiveRef primitive in primitives)
            bounds = bounds.Include(primitive.Bounds);
        return bounds;
    }

    private static AxisAlignedBoundingBox ComputeCentroidBounds(List<BvhPrimitiveRef> primitives)
    {
        AxisAlignedBoundingBox bounds = AxisAlignedBoundingBox.Empty;
        foreach (BvhPrimitiveRef primitive in primitives)
            bounds = bounds.Include(primitive.Bounds.Centroid);
        return bounds;
    }

    private static int GetBin(float centroid, float centroidMin, float centroidExtent) =>
        Math.Clamp((int)(((centroid - centroidMin) / centroidExtent) * BinCount), 0, BinCount - 1);

    private static int LargestAxis(Vector3 extent)
    {
        if (extent.X >= extent.Y && extent.X >= extent.Z)
            return 0;

        return extent.Y >= extent.Z ? 1 : 2;
    }
}

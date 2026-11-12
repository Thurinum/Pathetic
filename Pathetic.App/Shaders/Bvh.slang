module Bvh;

import Camera;
import Primitives;

public struct BvhIntersection
{
    public bool Hit;
    public float Near;
    public float Far;
};

public struct BvhNode
{
    float3 Min;
    float3 Max;
    uint4 ChildMin0;
    uint4 ChildMin1;
    uint4 ChildMax0;
    uint4 ChildMax1;
    uint4 ChildRef0;
    uint4 ChildRef1;
    public uint ChildCount;
    uint Padding;

    public static const uint Root = 0;

    uint GetLane(uint4 values, uint lane)
    {
        if (lane == 0) return values.x;
        if (lane == 1) return values.y;
        if (lane == 2) return values.z;
        return values.w;
    }

    uint GetChildMin(uint slot)
    {
        return GetLane(slot < 4 ? ChildMin0 : ChildMin1, slot & 3);
    }

    uint GetChildMax(uint slot)
    {
        return GetLane(slot < 4 ? ChildMax0 : ChildMax1, slot & 3);
    }

    public uint GetChildRef(uint slot)
    {
        return GetLane(slot < 4 ? ChildRef0 : ChildRef1, slot & 3);
    }

    public BvhIntersection IntersectsChild(Ray ray, float3 inverseDirection, uint slot, float tMin, float tMax)
    {
        uint packedMin = GetChildMin(slot);
        uint packedMax = GetChildMax(slot);
        float3 extent = (this.Max - this.Min) * (1.0 / 255.0);

        float3 childMin = this.Min + float3(
            packedMin & 255,
            (packedMin >> 8) & 255,
            (packedMin >> 16) & 255) * extent;
        float3 childMax = this.Min + float3(
            packedMax & 255,
            (packedMax >> 8) & 255,
            (packedMax >> 16) & 255) * extent;

        float3 travel0 = (childMin - ray.Origin) * inverseDirection;
        float3 travel1 = (childMax - ray.Origin) * inverseDirection;
        float3 near = min(travel0, travel1);
        float3 far = max(travel0, travel1);

        BvhIntersection result;
        result.Near = max(tMin, max(near.x, max(near.y, near.z)));
        result.Far = min(tMax, min(far.x, min(far.y, far.z)));
        result.Hit = result.Far >= result.Near;
        return result;
    }
};

public struct BvhPrimitiveRef
{
    public PrimitiveType Type;
    public uint Index;
};

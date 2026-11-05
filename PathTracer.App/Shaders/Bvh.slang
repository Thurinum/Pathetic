module Bvh;

import Environment;
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
    public uint Left;
    public uint Right;
    public uint FirstPrimitive;
    public uint PrimitiveCount;
    
    public static const uint Root = 0;
    
    public BvhIntersection Intersects(Ray ray, float travelMin, float travelMax)
    {
        float3 inverseDirection = 1.0f / ray.Direction;
        float3 travel0 = (this.Min - ray.Origin) * inverseDirection;
        float3 travel1 = (this.Max - ray.Origin) * inverseDirection;

        float3 near = min(travel0, travel1);
        float3 far = max(travel0, travel1);

        float hitNear = max(travelMin, max(near.x, max(near.y, near.z)));
        float hitFar = min(travelMax, min(far.x, min(far.y, far.z)));

        BvhIntersection result;
        result.Hit = hitFar >= hitNear;
        result.Near = hitNear;
        result.Far = hitFar;
        return result;
    }
};

public struct BvhPrimitiveRef
{
    public PrimitiveType Type;
    public uint Index;
};

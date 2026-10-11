module Bvh;

import Environment;
import Primitives;

public struct BvhNode
{
    float3 Min;
    float3 Max;
    public uint Left;
    public uint Right;
    public uint FirstPrimitive;
    public uint PrimitiveCount;
    
    public static const uint Root = 0;
    
    public bool Intersects(Ray ray, float travelMin, float travelMax)
    {
        float3 inverseDirection = 1.0f / ray.Direction;
        float3 travel0 = (this.Min - ray.Origin) * inverseDirection;
        float3 travel1 = (this.Max - ray.Origin) * inverseDirection;

        float3 near = min(travel0, travel1);
        float3 far = max(travel0, travel1);

        float travelMin = max(travelMin, max(near.x, max(near.y, near.z)));
        float travelMax = min(travelMax, min(far.x, min(far.y, far.z)));

        return travelMax >= travelMin;
    }
};

public struct BvhPrimitiveRef
{
    public PrimitiveType Type;
    public uint Index;
};


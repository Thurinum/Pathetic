module Bvh;

import Primitives;
import Camera;

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
        // Slab test. The inverse direction may contain infinities, which is fine for
        // non-zero direction components; rays parallel to a slab produce matching
        // infinities and are handled by the min/max operations.
        float3 inverseDirection = 1.0 / ray.Direction;
        float3 travel0 = (this.Min - ray.Origin) * inverseDirection;
        float3 travel1 = (this.Max - ray.Origin) * inverseDirection;

        float3 near = min(travel0, travel1);
        float3 far = max(travel0, travel1);

        float tMin = max(travelMin, max(near.x, max(near.y, near.z)));
        float tMax = min(travelMax, min(far.x, min(far.y, far.z)));

        return tMax >= tMin;
    }
}

public struct BvhPrimitiveRef
{
    public PrimitiveType Type;
    public uint Index;
}

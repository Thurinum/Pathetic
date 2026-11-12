module PathTracer;

import Primitives;
import Camera;
import Random;
import Accumulation;
import Environment;
import FrameState;
import Bvh;

struct Params
{
    uint PlaneCount;
    uint SphereCount;
    uint SamplesPerPixel;
    uint MaxBounces;
    uint BvhNodeCount;
}

RWTexture2D<float4> pathTracerColor;
ConstantBuffer<Camera> camera;
StructuredBuffer<Plane, ScalarDataLayout> planes;
StructuredBuffer<Sphere, ScalarDataLayout> spheres;
ConstantBuffer<Params> params;
StructuredBuffer<BvhNode, ScalarDataLayout> bvhNodes;
StructuredBuffer<BvhPrimitiveRef, ScalarDataLayout> bvhPrimitiveRefs;

public HitResult TraceBvh(Ray ray)
{
    if (params.BvhNodeCount == 0)
        return HitResult.Miss();

    HitResult bestHit = HitResult.Miss();
    float bestT = 1e30;
    float3 inverseDirection = 1.0 / ray.Direction;
    
    uint nodeStack[64];
    uint pendingMaskStack[64];
    uint stackSize = 0;
    uint nodeIndex = BvhNode.Root;
    uint activeMask = 0xff;
    bool processAllChildren = true;

    while (true)
    {
        BvhNode node = bvhNodes[nodeIndex];
        uint nearestInternal = 0;
        uint nearestSlot = 0;
        float nearestT = bestT;
        uint pendingMask = 0;

        for (uint child = 0; child < node.ChildCount; ++child)
        {
            if (!processAllChildren && (activeMask & (1u << child)) == 0)
                continue;

            BvhIntersection intersection = node.IntersectsChild(ray, inverseDirection, child, 1e-4, bestT);
            if (!intersection.Hit)
                continue;

            uint childRef = node.GetChildRef(child);
            if ((childRef & 0x80000000) != 0)
            {
                if (nearestInternal == 0 || intersection.Near < nearestT)
                {
                    if (nearestInternal != 0)
                        pendingMask |= 1u << nearestSlot;
                    nearestInternal = childRef;
                    nearestSlot = child;
                    nearestT = intersection.Near;
                }
                else
                {
                    pendingMask |= 1u << child;
                }
                continue;
            }

            if (!processAllChildren)
                continue;

            uint firstPrimitive = childRef & 0x1fffffff;
            uint primitiveCount = ((childRef >> 29) & 3) + 1;
            for (uint i = 0; i < primitiveCount; ++i)
            {
                BvhPrimitiveRef primitive = bvhPrimitiveRefs[firstPrimitive + i];

                HitResult hit = HitResult.Miss();
                switch (primitive.Type)
                {
                    case PrimitiveType.Plane:
                        Plane plane = planes[primitive.Index];
                        hit = IntersectPlane(ray.Origin, ray.Direction, plane, 1e-4, bestT);
                        break;
                    case PrimitiveType.Sphere:
                        Sphere sphere = spheres[primitive.Index];
                        hit = IntersectSphere(ray.Origin, ray.Direction, sphere.Center, sphere.Radius, 1e-4, bestT);
                        break;
                }

                if (hit.IsHit() && hit.T < bestT)
                {
                    bestT = hit.T;
                    bestHit = hit;
                    bestHit.Index = primitive.Index;
                }
            }
        }

        if (nearestInternal != 0)
        {
            if (pendingMask != 0)
            {
                nodeStack[stackSize] = nodeIndex;
                pendingMaskStack[stackSize] = pendingMask;
                stackSize++;
            }
            nodeIndex = nearestInternal & 0x7fffffff;
            activeMask = 0xff;
            processAllChildren = true;
            continue;
        }

        if (stackSize == 0)
            break;

        stackSize--;
        nodeIndex = nodeStack[stackSize];
        activeMask = pendingMaskStack[stackSize];
        processAllChildren = false;
    }

    return bestHit;
}

float3 Trace(Ray ray, inout Rng rng)
{
    float3 light = 0.0;
    float3 weight = 1.0;

    for (uint bounce = 0; bounce <= params.MaxBounces; ++bounce)
    {
        HitResult bestHit = TraceBvh(ray);
        
        if (bestHit.Primitive == PrimitiveType.Plane)
        {
            return float3(0.0, 1.0, 0.0);
        }

        if (!bestHit.IsHit())
        {
            light += weight * Sky(ray.Direction);
            break;
        }

        float3 albedo = UnpackRgb(spheres[bestHit.Index].Albedo);

        if (dot(bestHit.NormalExt, -ray.Direction) > 0.0)
            light += weight * UnpackRgb(spheres[bestHit.Index].Emission) * spheres[bestHit.Index].EmissionStrength;

        if (bounce == params.MaxBounces || all(albedo == 0.0))
            break;

        float3 newDirection = RandomBounceDirection(bestHit.Normal, rng);
        weight *= 2.0 * albedo * dot(bestHit.Normal, newDirection);
        ray.Origin += bestHit.T * ray.Direction + 1e-4 * bestHit.Normal;
        ray.Direction = newDirection;
    }
    return light;
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    pathTracerColor.GetDimensions(frameSize.x, frameSize.y);

    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;

    Rng rng = Rng(pixel, frameState.FrameIndex);
    Accumulator accum = Accumulator();

    for (uint k = 0; k < params.SamplesPerPixel; ++k)
    {
        float2 jitter = float2(rng.Next(), rng.Next());
        float3 C = Trace(BuildPrimaryRay(camera, rng, pixel, frameSize, jitter), rng);
        accum.Add(C);
    }

    pathTracerColor[pixel] = float4(accum.Average(), 1.0);
}

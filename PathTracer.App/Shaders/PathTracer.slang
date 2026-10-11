module PathTracer;

import Primitives;
import Camera;
import Random;
import Accumulation;
import Environment;
import Bvh;

struct Params
{
    uint PlaneCount;
    uint SphereCount;
    uint Seed;
    uint SamplesPerPixel;
    uint MaxBounces;
    uint BvhNodeCount;
}

RWTexture2D<float4> outputImage : register(u0);
Texture2D<float4> circles : register(t1);
SamplerState circlesSampler : register(s2);
ConstantBuffer<Camera> camera : register(b3);
StructuredBuffer<Plane, ScalarDataLayout> planes : register(t4);
StructuredBuffer<Sphere, ScalarDataLayout> spheres : register(t5);
ConstantBuffer<Params> params : register(b6);
StructuredBuffer<BvhNode, ScalarDataLayout> bvhNodes : register(t7);
StructuredBuffer<BvhPrimitiveRef, ScalarDataLayout> bvhPrimitiveRefs : register(t8);

public HitResult TraceBvh(Ray ray)
{
    if (params.BvhNodeCount == 0) {
        return HitResult.Miss();
    }

    HitResult bestHit = HitResult.Miss();
    float bestT = 1e30;

    uint stack[1024];
    uint stackSize = 0;
    stack[stackSize++] = BvhNode.Root;

    uint iterations = 0;
    while (stackSize > 0)
    {
        uint nodeIndex = stack[--stackSize];
        BvhNode node = bvhNodes[nodeIndex];

        if (!node.Intersects(ray, 1e-4, bestT))
            continue;

        if (node.PrimitiveCount > 0)
        {
            for (uint i = 0; i < node.PrimitiveCount; ++i)
            {
                BvhPrimitiveRef primitive = bvhPrimitiveRefs[node.FirstPrimitive + i];
                    
               HitResult hit = HitResult.Miss();
               switch (primitive.Type)
               {
                   case PrimitiveType.Plane:
                         hit = IntersectPlane(ray, planes[primitive.Index], 1e-4, bestT);
                         hit.Primitive = PrimitiveType.Plane;
                         hit.Index = primitive.Index;
                         break;
                   case PrimitiveType.Sphere:
                         hit = IntersectSphere(ray, spheres[primitive.Index], 1e-4, bestT);
                         hit.Primitive = PrimitiveType.Sphere;
                         hit.Index = primitive.Index;
                         break;
               }

                if (hit.IsHit())
                {
                    bestT = hit.T;
                    bestHit = hit;
                }
            }

            continue;
        }

        stack[stackSize++] = node.Left;
        stack[stackSize++] = node.Right;
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

        if (!bestHit.IsHit() || bestHit.Primitive == PrimitiveType.Plane)
        {
            light += weight * Sky(ray.Direction);
            break;
        }
        
        float3 albedo = spheres[bestHit.Index].Albedo;

        if (dot(bestHit.NormalExt, -ray.Direction) > 0.0)
            light += weight * spheres[bestHit.Index].Emission;

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
    outputImage.GetDimensions(frameSize.x, frameSize.y);
    
    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;
    
    Accumulator accum = Accumulator();
    
    Rng rng = Rng(pixel, params.Seed);
    
    for (uint k = 0; k < params.SamplesPerPixel; ++k)
    {
        float2 jitter = float2(rng.Next(), rng.Next());
        float3 C = Trace(BuildPrimaryRay(camera, rng, pixel, frameSize, jitter), rng);
        accum.Add(C);
    }

    outputImage[pixel] = float4(accum.Average(), 1.0);
}

module PathTracer;

import Primitives;
import Camera;
import Random;
import Accumulation;
import Environment;

struct Params
{
    uint PlaneCount;
    uint SphereCount;
    uint Seed;
    uint SamplesPerPixel;
    uint MaxBounces;
}

RWTexture2D<float4> outputImage : register(u0);
Texture2D<float4> circles : register(t1);
SamplerState circlesSampler : register(s2);
ConstantBuffer<Camera> camera : register(b3);
StructuredBuffer<Plane> planes : register(t4);
StructuredBuffer<Sphere> spheres : register(t5);
ConstantBuffer<Params> params : register(b6);

float3 Trace(Ray ray, inout Rng rng)
{
    float3 light = 0.0;
    float3 weight = 1.0;

    for (uint bounce = 0; bounce <= params.MaxBounces; ++bounce)
    {
        float bestT = 1e30;
        HitResult bestHit = HitResult.Miss();

        for (uint i = 0; i < params.SphereCount; ++i)
        {
            HitResult h = IntersectSphere(ray.Origin, ray.Direction, spheres[i].Center, spheres[i].Radius, 1e-4, bestT);
            if (h.IsHit()) { bestT = h.T; bestHit = h; bestHit.Index = i; }
        }

        if (!bestHit.IsHit())
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
    
    Rng rng = Rng(pixel, params.Seed);
    Accumulator accum = Accumulator();

    for (uint k = 0; k < params.SamplesPerPixel; ++k)
    {
        float2 jitter = float2(rng.Next(), rng.Next());
        float3 C = Trace(BuildPrimaryRay(camera, rng, pixel, frameSize, jitter), rng);
        accum.Add(C);
    }

    outputImage[pixel] = float4(accum.Average(), 1.0);
}

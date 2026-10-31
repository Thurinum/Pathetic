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
    uint SphereCount;
    uint TriangleCount;
    uint SamplesPerPixel;
    uint MaxBounces;
    uint BvhNodeCount;
}

RWTexture2D<float4> pathTracerColor;
ConstantBuffer<Camera> camera;
StructuredBuffer<Plane> planes;
StructuredBuffer<Sphere> spheres;
StructuredBuffer<Triangle> triangles;
ConstantBuffer<Params> params;
StructuredBuffer<BvhNode> bvhNodes;
StructuredBuffer<BvhPrimitiveRef> bvhPrimitiveRefs;

HitResult TraceBvh(Ray ray)
{
    if (params.BvhNodeCount == 0)
        return HitResult.Miss();

    HitResult bestHit = HitResult.Miss();
    float bestT = 1e30;

    uint stack[64];
    uint stackSize = 0;
    stack[stackSize++] = BvhNode.Root;

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
                        hit = IntersectPlane(ray.Origin, ray.Direction, planes[primitive.Index], 1e-4, bestT);
                        break;
                    case PrimitiveType.Sphere:
                        hit = IntersectSphere(ray.Origin, ray.Direction, spheres[primitive.Index].Center, spheres[primitive.Index].Radius, 1e-4, bestT);
                        break;
                    case PrimitiveType.Triangle:
                        hit = IntersectTriangle(ray.Origin, ray.Direction, triangles[primitive.Index], 1e-4, bestT);
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

        if (node.Left != uint(-1))
            stack[stackSize++] = node.Left;
        if (node.Right != uint(-1))
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
        HitResult hit = TraceBvh(ray);

        if (!hit.IsHit())
        {
            light += weight * Sky(ray.Direction);
            break;
        }

        float3 albedo = 0.0;
        float3 emission = 0.0;

        switch (hit.Primitive)
        {
            case PrimitiveType.Sphere:
                albedo = UnpackRgb(spheres[hit.Index].Albedo) / 255.0;
                emission = UnpackRgb(spheres[hit.Index].Emission);
                break;
            case PrimitiveType.Triangle:
                albedo = UnpackRgb(triangles[hit.Index].Albedo) / 255.0;
                emission = UnpackRgb(triangles[hit.Index].Emission);
                break;
            case PrimitiveType.Plane:
                albedo = float3(0.8, 0.8, 0.8);
                break;
            default:
                break;
        }

        if (dot(hit.NormalExt, -ray.Direction) > 0.0)
            light += weight * emission;

        if (bounce == params.MaxBounces || all(albedo == 0.0))
            break;

        float3 newDirection = RandomBounceDirection(hit.Normal, rng);
        weight *= 2.0 * albedo * dot(hit.Normal, newDirection);
        ray.Origin += hit.T * ray.Direction + 1e-4 * hit.Normal;
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

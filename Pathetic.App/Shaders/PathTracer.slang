module PathTracer;

import Primitives;
import Materials;
import Camera;
import Random;
import Accumulation;
import Environment;
import FrameState;
import Bvh;

#ifdef PROFILE_COUNTERS
struct PathTracerCounters
{
    uint Paths;
    uint Rays;
    uint RaysBounce1;
    uint RaysBounce2;
    uint RaysBounce3Plus;
    uint RootMisses;
    uint BoxTests;
    uint NodeVisits;
    uint NodeVisitSlots;
    uint PrimitiveTests;
    uint MaxStackDepth;
    uint SkyEscapes;
    uint Absorbed;
    uint EmitterHits;
    uint NanSamples;
};

RWStructuredBuffer<PathTracerCounters> profileCounters;
#endif

#include "ProfileCounters.slang"

struct Params
{
    uint PlaneCount;
    uint SphereCount;
    uint TriangleCount;
    uint SamplesPerPixel;
    uint MaxBounces;
    uint BvhNodeCount;
    uint DisplayNormals;
    uint SmoothShading;
}

struct BvhStackEntry
{
    uint Node;
    float Near;
};

RWTexture2D<float4> pathTracerColor;
ConstantBuffer<Camera> camera;
StructuredBuffer<Plane, ScalarDataLayout> planes;
StructuredBuffer<Sphere, ScalarDataLayout> spheres;
StructuredBuffer<Triangle, ScalarDataLayout> triangles;
StructuredBuffer<Material, ScalarDataLayout> materials;
ConstantBuffer<Params> params;
StructuredBuffer<BvhNode, ScalarDataLayout> bvhNodes;
StructuredBuffer<BvhPrimitiveRef, ScalarDataLayout> bvhPrimitiveRefs;

public HitResult TraceBvh(Ray ray)
{
    if (params.BvhNodeCount == 0)
        return HitResult.Miss();

    HitResult bestHit = HitResult.Miss();
    float bestT = 1e30;

    // Check if there is an intersection with the BVH at all
    BvhIntersection rootIntersection = bvhNodes[BvhNode.Root].Intersects(ray, 1e-4, bestT);
    PROFILE_ADD(BoxTests, 1);
    if (!rootIntersection.Hit)
    {
        PROFILE_ADD(RootMisses, 1);
        return bestHit;
    }

    BvhStackEntry stack[64];
    uint stackSize = 0;
    stack[stackSize++] = { BvhNode.Root, rootIntersection.Near };

    while (stackSize > 0)
    {
        PROFILE_MAX(MaxStackDepth, stackSize);
        BvhStackEntry entry = stack[--stackSize];

        // Discard nodes whose entry point is past the closest hit found so far
        if (entry.Near > bestT)
            continue;

        BvhNode node = bvhNodes[entry.Node];
        PROFILE_ADD(NodeVisits, 1);
        PROFILE_WAVE_SLOTS(NodeVisitSlots);

        if (node.PrimitiveCount > 0)
        {
            for (uint i = 0; i < node.PrimitiveCount; ++i)
            {
                PROFILE_ADD(PrimitiveTests, 1);
                BvhPrimitiveRef primitive = bvhPrimitiveRefs[node.FirstPrimitive + i];

                HitResult hit = HitResult.Miss();
                switch (primitive.Type)
                {
                    case PrimitiveType.Plane:
                        hit = IntersectPlane(ray.Origin, ray.Direction, planes[primitive.Index], 1e-4, bestT);
                        break;
                    case PrimitiveType.Sphere:
                        hit = IntersectSphere(ray.Origin, ray.Direction, spheres[primitive.Index], 1e-4, bestT);
                        break;
                    case PrimitiveType.Triangle:
                        hit = IntersectTriangle(ray.Origin, ray.Direction, triangles[primitive.Index], 1e-4, bestT);
                        break;
                }

                if (hit.IsHit() && hit.T < bestT)
                {
                    bestT = hit.T;
                    bestHit = hit;
                    bestHit.Index = primitive.Index;
                }
            }

            continue;
        }

        PROFILE_ADD(BoxTests, 2);
        BvhIntersection leftIntersection = bvhNodes[node.Left].Intersects(ray, 1e-4, bestT);
        BvhIntersection rightIntersection = bvhNodes[node.Right].Intersects(ray, 1e-4, bestT);

        if (leftIntersection.Hit && !rightIntersection.Hit)
        {
            stack[stackSize++] = { node.Left, leftIntersection.Near };
        }
        else if (!leftIntersection.Hit && rightIntersection.Hit)
        {
            stack[stackSize++] = { node.Right, rightIntersection.Near };
        }
        else if (leftIntersection.Hit && rightIntersection.Hit)
        {
            // Put nearest intersection on the stack last
            bool leftIsNearer = leftIntersection.Near < rightIntersection.Near;
            stack[stackSize++] = { leftIsNearer ? node.Right : node.Left, leftIsNearer ? rightIntersection.Near : leftIntersection.Near };
            stack[stackSize++] = { leftIsNearer ? node.Left : node.Right, leftIsNearer ? leftIntersection.Near : rightIntersection.Near };
        }
    }

    return bestHit;
}

float3 Trace(Ray ray, inout Rng rng)
{
    float3 light = 0.0;
    float3 weight = 1.0;

    for (uint bounce = 0; bounce < params.MaxBounces; ++bounce)
    {
        PROFILE_ADD(Rays, 1);
        PROFILE_ADD(RaysBounce1, bounce == 1 ? 1 : 0);
        PROFILE_ADD(RaysBounce2, bounce == 2 ? 1 : 0);
        PROFILE_ADD(RaysBounce3Plus, bounce > 2 ? 1 : 0);
        HitResult bestHit = TraceBvh(ray);

        if (!bestHit.IsHit())
        {
            PROFILE_ADD(SkyEscapes, 1);
            light += weight * Sky(ray.Direction);
            break;
        }

        Material material = materials[bestHit.MaterialId];
        float3 albedo = material.Albedo.UnpackRgb();

        float3 shadingNormal = params.SmoothShading != 0 ? bestHit.NormalInterp : bestHit.Normal;

        if (params.DisplayNormals != 0)
            return 0.5 * (shadingNormal + 1.0);

        if (dot(bestHit.NormalExt, -ray.Direction) > 0.0)
        {
            float3 emission = material.Emission.UnpackRgb() * material.EmissionStrength;
            light += weight * emission;
            if (any(emission > 0.0))
            {
                PROFILE_ADD(EmitterHits, 1);
            }
        }

        if (all(albedo == 0.0))
        {
            PROFILE_ADD(Absorbed, 1);
            break;
        }

        float3 newDirection = RandomBounceDirection(shadingNormal, rng);
        weight *= albedo;

        if (all(weight == 0.0))
            break;

        if (bounce >= 3)
        {
            float continuationProbability = min(0.95, max(0.05, max(weight.r, max(weight.g, weight.b))));
            if (rng.Next() >= continuationProbability)
                break;
            weight /= continuationProbability;
        }

        ray.Origin += bestHit.T * ray.Direction + 1e-4 * bestHit.Normal;
        ray.Direction = newDirection;
    }
    return light;
}

void RenderPixel(uint2 pixel, uint2 frameSize)
{
    Rng rng = Rng(pixel, frameState.FrameIndex);
    Accumulator accum = Accumulator();

    uint cellsPerAxis = clamp((uint)floor(sqrt((float)params.SamplesPerPixel)), 1u, 8u);
    uint sampleCount = cellsPerAxis * cellsPerAxis;
    uint apertureCells[64];
    ShuffleCells(apertureCells, sampleCount, rng);

    for (uint sampleIndex = 0; sampleIndex < sampleCount; ++sampleIndex)
    {
        float2 pixelPoint = StratifiedPoint(sampleIndex, cellsPerAxis, rng);
        float2 aperturePoint = StratifiedPoint(apertureCells[sampleIndex], cellsPerAxis, rng);
        float3 C = Trace(BuildPrimaryRay(camera, pixel, frameSize, pixelPoint, aperturePoint), rng);
        PROFILE_ADD(Paths, 1);
        if (any(isnan(C)))
        {
            PROFILE_ADD(NanSamples, 1);
        }
        accum.Add(C);
    }

    pathTracerColor[pixel] = float4(accum.Average(), 1.0);
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID, uint groupIndex : SV_GroupIndex)
{
    uint2 frameSize;
    pathTracerColor.GetDimensions(frameSize.x, frameSize.y);

    uint2 pixel = dispatchThreadId.xy;
    if (all(pixel < frameSize))
        RenderPixel(pixel, frameSize);
}

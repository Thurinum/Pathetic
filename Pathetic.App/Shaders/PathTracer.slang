module PathTracer;

import Primitives;
import Materials;
import Camera;
import Random;
import Accumulation;
import Environment;
import FrameState;
import Bvh;

struct Params
{
    uint PrimitiveCount;
    uint SamplesPerPixel;
    uint MaxBounces;
    uint BvhNodeCount;
}

RWTexture2D<float4> pathTracerColor;
ConstantBuffer<Camera> camera;
StructuredBuffer<Primitive, ScalarDataLayout> primitives;
StructuredBuffer<Material, ScalarDataLayout> materials;
ConstantBuffer<Params> params;
StructuredBuffer<BvhNode, ScalarDataLayout> bvhNodes;
StructuredBuffer<BvhPrimitiveRef, ScalarDataLayout> bvhPrimitiveRefs;

public HitResult TraceBvh(Ray ray, out Primitive hitPrimitive)
{
    hitPrimitive = { PrimitiveType.Plane, float3(0.0), 0, 0 };

    if (params.BvhNodeCount == 0)
        return HitResult.Miss();

    HitResult bestHit = HitResult.Miss();
    float bestT = 1e30;
    
    // Check if there is an intersection with the BVH at all
    BvhIntersection rootIntersection = bvhNodes[BvhNode.Root].Intersects(ray, 1e-4, bestT);
    if (!rootIntersection.Hit)
        return bestHit;

    uint stack[32];
    uint stackSize = 0;
    stack[stackSize++] = BvhNode.Root;

    while (stackSize > 0)
    {
        uint nodeIndex = stack[--stackSize];
        BvhNode node = bvhNodes[nodeIndex];

        if (node.PrimitiveCount > 0)
        {
            for (uint i = 0; i < node.PrimitiveCount; ++i)
            {
                BvhPrimitiveRef primitive = bvhPrimitiveRefs[node.FirstPrimitive + i];
                Primitive candidate = primitives[primitive.Index];

                HitResult hit = IntersectPrimitive(ray.Origin, ray.Direction, candidate, 1e-4, bestT);

                if (hit.IsHit() && hit.T < bestT)
                {
                    bestT = hit.T;
                    bestHit = hit;
                    bestHit.Index = primitive.Index;
                    hitPrimitive = candidate;
                }
            }

            continue;
        }

        BvhIntersection leftIntersection = bvhNodes[node.Left].Intersects(ray, 1e-4, bestT);
        BvhIntersection rightIntersection = bvhNodes[node.Right].Intersects(ray, 1e-4, bestT);

        if (leftIntersection.Hit && !rightIntersection.Hit)
        {
            stack[stackSize++] = node.Left;
        }
        else if (!leftIntersection.Hit && rightIntersection.Hit)
        {
            stack[stackSize++] = node.Right;
        }
        else if (leftIntersection.Hit && rightIntersection.Hit)
        {
            // Put nearest intersection on the stack last
            bool leftIsNearer = leftIntersection.Near < rightIntersection.Near;
            stack[stackSize++] = leftIsNearer ? node.Right : node.Left;
            stack[stackSize++] = leftIsNearer ? node.Left : node.Right;
        }
    }

    return bestHit;
}

float3 Trace(Ray ray, inout Rng rng)
{
    float3 light = 0.0;
    float3 weight = 1.0;

    for (uint bounce = 0; ; ++bounce)
    {
        Primitive hitPrimitive;
        HitResult bestHit = TraceBvh(ray, hitPrimitive);

        if (!bestHit.IsHit())
        {
            light += weight * Sky(ray.Direction);
            break;
        }

        Material material = materials[hitPrimitive.MaterialId];
        float3 albedo = UnpackRgb(material.Albedo);

        if (dot(bestHit.NormalExt, -ray.Direction) > 0.0)
            light += weight * UnpackRgb(material.Emission) * material.EmissionStrength;

        if (all(albedo == 0.0))
            break;

        float3 newDirection = RandomBounceDirection(bestHit.Normal, rng);
        weight *= 2.0 * albedo * dot(bestHit.Normal, newDirection);
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

    uint cellsPerAxis = clamp((uint)floor(sqrt((float)params.SamplesPerPixel)), 1u, 8u);
    uint sampleCount = cellsPerAxis * cellsPerAxis;
    uint apertureCells[64];
    ShuffleCells(apertureCells, sampleCount, rng);

    for (uint sampleIndex = 0; sampleIndex < sampleCount; ++sampleIndex)
    {
        float2 pixelPoint = StratifiedPoint(sampleIndex, cellsPerAxis, rng);
        float2 aperturePoint = StratifiedPoint(apertureCells[sampleIndex], cellsPerAxis, rng);
        accum.Add(Trace(BuildPrimaryRay(camera, pixel, frameSize, pixelPoint, aperturePoint), rng));
    }

    pathTracerColor[pixel] = float4(accum.Average(), 1.0);
}

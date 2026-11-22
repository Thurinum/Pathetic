module PathTracer;

import CPUType;

import Primitives;
import Materials;
import Camera;
import Random;
import Accumulation;
import Environment;
import FrameState;
import Bvh;

[CPUType("PathTracerParams")]
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
StructuredBuffer<Plane> planes;
StructuredBuffer<Sphere> spheres;
StructuredBuffer<Triangle> triangles;
StructuredBuffer<Material> materials;
ConstantBuffer<Params> params;
StructuredBuffer<BvhNode> bvhNodes;
StructuredBuffer<BvhPrimitiveRef> bvhPrimitiveRefs;

public HitResult TraceBvh(Ray ray)
{
    if (params.BvhNodeCount == 0)
        return HitResult.Miss();

    HitResult bestHit = HitResult.Miss();
    float bestT = 1e30;

    // Check if there is an intersection with the BVH at all
    BvhIntersection rootIntersection = bvhNodes[BvhNode.Root].Intersects(ray, 1e-4, bestT);
    if (!rootIntersection.Hit)
        return bestHit;

    BvhStackEntry stack[64];
    uint stackSize = 0;
    stack[stackSize++] = { BvhNode.Root, rootIntersection.Near };

    while (stackSize > 0)
    {
        BvhStackEntry entry = stack[--stackSize];

        // Discard nodes whose entry point is past the closest hit found so far
        if (entry.Near > bestT)
            continue;

        BvhNode node = bvhNodes[entry.Node];

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
        HitResult bestHit = TraceBvh(ray);

        if (!bestHit.IsHit())
        {
            light += weight * Sky(ray.Direction);
            break;
        }

        Material material = materials[bestHit.MaterialId];
        float3 albedo = material.Albedo.UnpackRgb();

        float3 shadingNormal = params.SmoothShading != 0 ? bestHit.NormalInterp : bestHit.Normal;

        if (params.DisplayNormals != 0)
            return 0.5 * (shadingNormal + 1.0);

        if (dot(bestHit.NormalExt, -ray.Direction) > 0.0)
            light += weight * material.Emission.UnpackRgb() * material.EmissionStrength;

        if (all(albedo == 0.0))
            break;

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

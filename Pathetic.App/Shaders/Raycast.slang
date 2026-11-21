module Raycast;

import Primitives;

struct RayRequest
{
    float3 Origin;
    float3 Direction;
}

struct Params
{
    RayRequest Request;
    uint PlaneCount;
    uint SphereCount;
    uint TriangleCount;
}

struct RaycastHit
{
    float T;
    uint Primitive;
    uint Index;
    uint _Pad;
}

ConstantBuffer<Params> raycastParams;
StructuredBuffer<Plane, ScalarDataLayout> planes;
StructuredBuffer<Sphere, ScalarDataLayout> spheres;
StructuredBuffer<Triangle, ScalarDataLayout> triangles;
RWStructuredBuffer<RaycastHit> raycastHits;

[shader("compute")]
[numthreads(1, 1, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    RayRequest ray = raycastParams.Request;

    RaycastHit best;
    best.T = 1e30;
    best.Primitive = 1000;
    best.Index = 0;
    best._Pad = 0;

    for (uint i = 0; i < raycastParams.PlaneCount; ++i)
    {
        HitResult hit = IntersectPlane(ray.Origin, ray.Direction, planes[i], 1e-4, best.T);
        if (hit.IsHit())
        {
            best.T = hit.T;
            best.Primitive = 0;
            best.Index = i;
        }
    }

    for (uint i = 0; i < raycastParams.SphereCount; ++i)
    {
        Sphere sphere = spheres[i];
        HitResult hit = IntersectSphere(ray.Origin, ray.Direction, sphere, 1e-4, best.T);
        if (hit.IsHit())
        {
            best.T = hit.T;
            best.Primitive = 1;
            best.Index = i;
        }
    }

    for (uint i = 0; i < raycastParams.TriangleCount; ++i)
    {
        HitResult hit = IntersectTriangle(ray.Origin, ray.Direction, triangles[i], 1e-4, best.T);
        if (hit.IsHit())
        {
            best.T = hit.T;
            best.Primitive = 2;
            best.Index = i;
        }
    }

    raycastHits[0] = best;
}

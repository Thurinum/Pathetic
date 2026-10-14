module ObjectId;

import Primitives;
import Camera;

struct Params
{
    uint SphereCount;
}

RWTexture2D<uint> objectId;
ConstantBuffer<Camera> camera;
StructuredBuffer<Sphere> spheres;
ConstantBuffer<Params> params;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    objectId.GetDimensions(frameSize.x, frameSize.y);

    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;

    // Deterministic (jitter-free, aperture-free) primary ray so the id buffer is stable across frames.
    float aspect = frameSize.x / (float)frameSize.y;
    float2 uv = (float2(pixel) + 0.5) / float2(frameSize);
    float viewPlaneX = (2.0 * uv.x - 1.0) * aspect * camera.TanHalfFov;
    float viewPlaneY = (1.0 - 2.0 * uv.y) * camera.TanHalfFov;
    float3 direction = normalize(camera.Forward + viewPlaneX * camera.Right + viewPlaneY * camera.Up);

    float bestT = 1e30;
    uint best = 0u;

    for (uint i = 0; i < params.SphereCount; ++i)
    {
        HitResult h = IntersectSphere(camera.Origin, direction, spheres[i].Center, spheres[i].Radius, 1e-4, bestT);
        if (h.IsHit())
        {
            bestT = h.T;
            best = spheres[i].ObjectId;
        }
    }

    objectId[pixel] = best;
}

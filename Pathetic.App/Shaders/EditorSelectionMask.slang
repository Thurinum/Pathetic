module EditorSelectionMask;

import Primitives;
import Camera;

struct Params
{
    uint Count;
}

struct Selection
{
    uint Primitive;
    uint Index;
}

RWTexture2D<uint> selectionMask;
ConstantBuffer<Params> params;
ConstantBuffer<Camera> camera;
StructuredBuffer<Selection> selections;
StructuredBuffer<Plane, ScalarDataLayout> planes;
StructuredBuffer<Sphere, ScalarDataLayout> spheres;

static const uint INDEX_NONE = ~0u;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    selectionMask.GetDimensions(frameSize.x, frameSize.y);

    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;

    float2 screen = (float2(pixel) + 0.5) / float2(frameSize);
    float3 direction = normalize(BuildViewPlaneDirection(camera, screen, frameSize));

    uint bestIdx = INDEX_NONE;
    float bestT = 1e30;
    for (uint i = 0; i < params.Count; ++i)
    {
        Selection selection = selections[i];
        HitResult hit = HitResult.Miss();

        if (selection.Primitive == PrimitiveType.Sphere)
        {
            Sphere sphere = spheres[selection.Index];
            hit = IntersectSphere(camera.Origin, direction, sphere.Center, sphere.Radius, 1e-4, 1e30);
        }
        else if (selection.Primitive == PrimitiveType.Plane)
        {
            Plane plane = planes[selection.Index];
            hit = IntersectPlane(camera.Origin, direction, plane, 1e-4, 1e30);
        }

        if (hit.IsHit() && hit.T < bestT)
        {
            bestT = hit.T;
            bestIdx = i;
        }
    }

    selectionMask[pixel] = bestIdx == INDEX_NONE ? 0u : bestIdx + 1u;
}

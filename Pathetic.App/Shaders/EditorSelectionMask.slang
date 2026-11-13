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

RWTexture2D<float4> color;
ConstantBuffer<Params> params;
ConstantBuffer<Camera> camera;
StructuredBuffer<Selection> selections;
StructuredBuffer<Plane, ScalarDataLayout> planes;
StructuredBuffer<Sphere, ScalarDataLayout> spheres;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    color.GetDimensions(frameSize.x, frameSize.y);

    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;

    float2 screen = (float2(pixel) + 0.5) / float2(frameSize);
    float aspect = float(frameSize.x) / float(frameSize.y);
    float viewX = (2.0 * screen.x - 1.0) * aspect * camera.TanHalfFov;
    float viewY = (1.0 - 2.0 * screen.y) * camera.TanHalfFov;
    float3 direction = normalize(camera.Forward + viewX * camera.Right + viewY * camera.Up);

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

        if (hit.IsHit())
        {
            color[pixel] = float4(SelectionColor(i), 1.0);
        }
    }
}

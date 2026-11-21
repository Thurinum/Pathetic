module EditorSelectionOutline;

Texture2D<uint> selectionMask;
RWTexture2D<float4> color;

static const float3 OutlineColor = float3(1.0, 0.55, 0.15);
static const int Thickness = 1;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    color.GetDimensions(frameSize.x, frameSize.y);

    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;

    int owner = selectionMask.Load(int3(int2(pixel), 0));
    bool outline = false;
    for (int y = -Thickness; y <= Thickness; ++y)
    {
        for (int x = -Thickness; x <= Thickness; ++x)
        {
            if (x == 0 && y == 0)
                continue;

            int2 neighbour = int2(pixel) + int2(x, y);
            if (any(neighbour < int2(0)) || any(neighbour >= int2(frameSize)))
                continue;

            if (selectionMask.Load(int3(neighbour, 0)) != owner)
            {
                outline = true;
            }
        }
    }

    if (outline)
    {
        color[pixel] = float4(OutlineColor, 1.0);
    }
}

module SelectionOutline;

struct Params
{
    uint SelectedObjectId;
}

RWTexture2D<float4> colorOutlined;
Texture2D<float4> color;
Texture2D<uint> objectId;
ConstantBuffer<Params> params;

static const float3 OutlineColor = float3(1.0, 0.6, 0.15);

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    color.GetDimensions(frameSize.x, frameSize.y);

    int2 pixel = int2(dispatchThreadId.xy);
    if (pixel.x >= int(frameSize.x) || pixel.y >= int(frameSize.y))
        return;

    float4 baseColor = color.Load(int3(pixel, 0));

    uint selected = params.SelectedObjectId;
    if (selected == 0u)
    {
        colorOutlined[pixel] = baseColor;
        return;
    }

    int2 size = int2(frameSize);
    bool center = objectId.Load(int3(pixel, 0)) == selected;
    bool neighbor = false;
    int2 probe;

    // A pixel is on the outline when it is selected but a 4-neighbour is not, or the reverse.
    probe = pixel + int2(1, 0);
    if (probe.x < size.x) neighbor = neighbor || objectId.Load(int3(probe, 0)) == selected;
    probe = pixel + int2(-1, 0);
    if (probe.x >= 0) neighbor = neighbor || objectId.Load(int3(probe, 0)) == selected;
    probe = pixel + int2(0, 1);
    if (probe.y < size.y) neighbor = neighbor || objectId.Load(int3(probe, 0)) == selected;
    probe = pixel + int2(0, -1);
    if (probe.y >= 0) neighbor = neighbor || objectId.Load(int3(probe, 0)) == selected;

    colorOutlined[pixel] = (center != neighbor) ? float4(OutlineColor, 1.0) : baseColor;
}

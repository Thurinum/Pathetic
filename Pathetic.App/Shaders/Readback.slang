module Readback;

Texture2D<float4> presentColor;
RWStructuredBuffer<uint> pixels;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint width;
    uint height;
    presentColor.GetDimensions(width, height);

    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= uint2(width, height)))
        return;

    float4 color = saturate(presentColor.Load(int3(pixel, 0)));
    uint4 bytes = uint4(round(color * 255.0));

    // Little-endian RGBA, matching the raw file consumed by the Blender add-on.
    pixels[pixel.y * width + pixel.x] = bytes.x | (bytes.y << 8) | (bytes.z << 16) | (bytes.w << 24);
}

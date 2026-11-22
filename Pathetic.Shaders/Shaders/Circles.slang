struct Circle 
{
    float2 Position01;
    float Radius;
}

struct Params
{
    uint Count;
}

RWTexture2D<float4> circles;
StructuredBuffer<Circle> circlePrimitives;
ConstantBuffer<Params> params;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    circles.GetDimensions(frameSize.x, frameSize.y);
    
    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;
        
    float2 uv = float2(pixel) / float2(frameSize);
    float3 color = float3(uv.x, uv.y, 0.5);
    
    float3 additive = 0.0;
    for (uint i = 0; i < params.Count; ++i)
    {
        Circle circle = circlePrimitives[i];
        float2 center = circle.Position01 * frameSize;
        float2 d = pixel - center;
        if (dot(d, d) <= circle.Radius * circle.Radius)
        {
            additive += color;
        }
    }
    
    circles[pixel] = float4(additive, 1.0);
}

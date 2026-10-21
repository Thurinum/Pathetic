module TemporalAccumulation;
import Tonemap;

struct Params 
{ 
    uint Reset; 
    uint Tonemapper;
    float Exposure;
}

RWTexture2D<float4> color;
Texture2D<float4> pathTracerColor;
RWTexture2D<float4> accumImage;
ConstantBuffer<Params> params;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    color.GetDimensions(frameSize.x, frameSize.y);
    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize)) 
        return;

    float3 estimate = pathTracerColor.Load(int3(pixel, 0)).rgb;

    float4 accum = (params.Reset != 0) ? float4(0.0) : accumImage[pixel];
    accum += float4(estimate, 1.0);
    accumImage[pixel] = accum;
    
    float3 hdr = accum.rgb / accum.a;
    hdr *= exp2(params.Exposure);
    float3 ldr = Tonemap(hdr, (Tonemapper)params.Tonemapper);
    color[pixel] = float4(LinearToSRGB(ldr), 1.0);
}

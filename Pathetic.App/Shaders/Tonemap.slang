module Tonemap;

public float3 Reinhard(float3 hdr)
{
    float3 ldr = hdr / (1.0 + hdr);
    float3 srgb = pow(ldr, 1.0 / 2.2);
    return srgb;
}

public float3 ACES(float3 x)
{
    const float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
    return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}
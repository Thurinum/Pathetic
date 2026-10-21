module Tonemap;

// sync with Pathetic.App.RenderPasses.Tonemapper.
// TODO: reflect this enum to generate the ImGui
public enum Tonemapper 
{
    None = 0,
    Reinhard = 1,
    ACES = 2
}

public float3 Reinhard(float3 hdr)
{
    return hdr / (1.0 + hdr);
}

public float3 ACES(float3 hdr)
{
    const float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
    return saturate((hdr * (a * hdr + b)) / (hdr * (c * hdr + d) + e));
}

public float3 Tonemap(float3 hdr, Tonemapper mode)
{
    if (mode == Tonemapper.Reinhard)
        return Reinhard(hdr);

    if (mode == Tonemapper.ACES)
        return ACES(hdr);

    return hdr;
}

public float3 LinearToSRGB(float3 linear)
{
    linear = max(linear, float3(0.0));
    
    float3 low = linear * 12.92;
    float3 high = 1.055 * pow(linear, 1.0 / 2.4) - 0.055;

    return select(linear <= float3(0.0031308), low, high);
}

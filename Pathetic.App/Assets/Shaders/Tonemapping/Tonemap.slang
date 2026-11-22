module Tonemap;
import AgX;

// sync with Pathetic.App.RenderPasses.Tonemapper.
// TODO: reflect this enum to generate the ImGui
public enum Tonemapper 
{
    None = 0,
    Reinhard = 1,
    AGX = 3
}

public float3 Reinhard(float3 hdr)
{
    return hdr / (1.0 + hdr);
}

public float3 Tonemap(float3 hdr, Tonemapper mode)
{
    if (mode == Tonemapper.Reinhard)
        return Reinhard(hdr);

    if (mode == Tonemapper.AGX)
        return AgX(hdr, AGX_PUNCHY);

    return hdr;
}

public float3 LinearToSRGB(float3 linear)
{
    linear = max(linear, float3(0.0));
    
    float3 low = linear * 12.92;
    float3 high = 1.055 * pow(linear, 1.0 / 2.4) - 0.055;

    return select(linear <= float3(0.0031308), low, high);
}

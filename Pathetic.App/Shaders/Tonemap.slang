module Tonemap;

// sync with Pathetic.App.RenderPasses.Tonemapper.
// TODO: reflect this enum to generate the ImGui
public enum Tonemapper 
{
    None = 0,
    Reinhard = 1,
    ACES = 2,
    AGX = 3
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

// AgX implementation adapted from Benjamin Wrensch, Troy Sobotka:
// https://iolite-engine.com/blog_posts/minimal_agx_implementation (MIT License)
public float3 AgX(float3 hdr)
{
    const float3x3 inset = float3x3(
        0.842479062253094,  0.0423282422610123, 0.0423756549057051,
        0.0784335999999992, 0.878468636469772,  0.0784336,
        0.0792237451477643, 0.0791661274605434, 0.879142973793104);

    const float3x3 outset = float3x3(
        1.19687900512017,   -0.0528968517574562, -0.0529716355144438,
       -0.0980208811401368,  1.15190312990417,   -0.0980434501171241,
       -0.0990297440797205, -0.0989611768448433,  1.15107367264116);

    const float min_ev = -12.47393;
    const float max_ev = 4.026069;

    hdr = max(hdr, float3(0.0));

    // Inset into the AgX working space, then log2-encode and normalize.
    hdr = mul(hdr, inset);
    hdr = clamp(log2(hdr), min_ev, max_ev);
    hdr = (hdr - min_ev) / (max_ev - min_ev);

    // Sigmoid approximation.
    float3 x2 = hdr * hdr;
    float3 x4 = x2 * x2;
    hdr = 15.5 * x4 * x2 - 40.14 * x4 * hdr + 31.96 * x4
        - 6.868 * x2 * hdr + 0.4298 * x2 + 0.1191 * hdr - 0.00232;
    hdr = saturate(hdr);

    // "Punchy" look; remove these three lines for the neutral look.
    float luma = dot(hdr, float3(0.2126, 0.7152, 0.0722));
    hdr = pow(hdr, float3(1.35));
    hdr = luma + 1.4 * (hdr - luma);

    // Back to sRGB primaries, then linearize for the sRGB encode pass.
    hdr = mul(hdr, outset);
    return pow(max(hdr, float3(0.0)), 2.2);
}

public float3 Tonemap(float3 hdr, Tonemapper mode)
{
    if (mode == Tonemapper.Reinhard)
        return Reinhard(hdr);

    if (mode == Tonemapper.ACES)
        return ACES(hdr);

    if (mode == Tonemapper.AGX)
        return AgX(hdr);

    return hdr;
}

public float3 LinearToSRGB(float3 linear)
{
    linear = max(linear, float3(0.0));
    
    float3 low = linear * 12.92;
    float3 high = 1.055 * pow(linear, 1.0 / 2.4) - 0.055;

    return select(linear <= float3(0.0031308), low, high);
}

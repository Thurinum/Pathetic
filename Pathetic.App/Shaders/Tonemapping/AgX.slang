// MIT License
// 
// Copyright (c) 2024 Missing Deadlines (Benjamin Wrensch)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// Ported to Slang from "Minimal AgX Implementation": https://iolite-engine.com/blog_posts/minimal_agx_implementation
// Algorithm: Troy Sobotka's AgX (https://github.com/sobotka/AgX).
module AgX;

public struct AgXLook
{
    public float3 Offset;
    public float3 Slope;
    public float3 Power;
    public float Saturation;
}

public static const AgXLook AGX_DEFAULT = { float3(0.0), float3(1.0), float3(1.0), 1.0 };
public static const AgXLook AGX_PUNCHY  = { float3(0.0), float3(1.0), float3(1.35, 1.35, 1.35), 1.4 };
public static const AgXLook AGX_GOLDEN  = { float3(0.0), float3(1.0, 0.9, 0.5), float3(0.8), 0.8 };

// AgX implementation adapted from Benjamin Wrensch, Troy Sobotka:
// https://iolite-engine.com/blog_posts/minimal_agx_implementation (MIT License)
public float3 AgX(float3 hdr, AgXLook look)
{
    const float3x3 inset = float3x3(
        0.842479062253094,  0.0423282422610123, 0.0423756549057051,
        0.0784335999999992, 0.878468636469772,  0.0784336,
        0.0792237451477643, 0.0791661274605434, 0.879142973793104);
    
    // the inverse of inset
    const float3x3 outset = float3x3(
        1.19687900512017,   -0.0528968517574562, -0.0529716355144438,
       -0.0980208811401368,  1.15190312990417,   -0.0980434501171241,
       -0.0990297440797205, -0.0989611768448433,  1.15107367264116);

    const float min_ev = -12.47393;
    const float max_ev = 4.026069;

    hdr = max(hdr, float3(0.0));

    // inset into the AgX working space, then log2-encode and normalize.
    hdr = mul(hdr, inset);
    hdr = clamp(log2(hdr), min_ev, max_ev);
    hdr = (hdr - min_ev) / (max_ev - min_ev);

    // sigmoid approximation
    float3 x2 = hdr * hdr;
    float3 x4 = x2 * x2;
    hdr = 15.5 * x4 * x2 
        - 40.14 * x4 * hdr 
        + 31.96 * x4
        - 6.868 * x2 * hdr 
        + 0.4298 * x2 
        + 0.1191 * hdr 
        - 0.00232;
    hdr = saturate(hdr);

    hdr = ApplyLook(hdr, look);

    // Back to sRGB primaries, then linearize for the sRGB encode pass.
    hdr = mul(hdr, outset);
    return pow(max(hdr, float3(0.0)), 2.2);
}

float3 ApplyLook(float3 val, AgXLook look)
{
    val = pow(max(val * look.Slope + look.Offset, 0.0), look.Power);
    float luma = dot(val, float3(0.2126, 0.7152, 0.0722));
    return luma + look.Saturation * (val - luma);
}
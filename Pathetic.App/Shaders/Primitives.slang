module Primitives;

// Values are mirrored by Pathetic.App.Bvh.PrimitiveType.
public enum PrimitiveType : uint
{
    Plane = 0u,
    Sphere = 1u,
    Triangle = 2u,
    None = 1000u,
}

public struct Sphere
{
    public float3 Center;
    public float Radius;
    public uint Albedo;   // packed RgbaByte
    public uint Emission; // packed RgbaByte
}

public struct Plane
{
    public float3 Center;
    public float3 U;
    public float3 V;
}

public struct Triangle
{
    public float3 V0;
    public float3 V1;
    public float3 V2;
    public uint Albedo;   // packed RgbaByte
    public uint Emission; // packed RgbaByte
}

public struct HitResult
{
    public float T;
    public float3 Normal;
    public float3 NormalExt;
    public float2 UV;
    public uint Index;
    public PrimitiveType Primitive;

    public bool IsHit() { return T > 0; }

    public static HitResult Miss()
    {
        HitResult h;
        h.T = -1;
        h.Normal = 0;
        h.NormalExt = 0;
        h.UV = 0;
        h.Index = 0;
        h.Primitive = PrimitiveType.None;
        return h;
    }
}

// Unpacks a packed RgbaByte into its 0..255 components (order-independent of /255 below).
public float3 UnpackRgb(uint packed)
{
    return float3(float(packed & 0xFFu), float((packed >> 8) & 0xFFu), float((packed >> 16) & 0xFFu));
}

public HitResult IntersectSphere(float3 origin, float3 dir, float3 center, float radius, float tMin, float tMax)
{
    float3 q = origin - center;
    float a = dot(dir, dir);
    float b = 2 * dot(q, dir);
    float c = dot(q, q) - (radius * radius);
    float discriminant = (b * b) - (4 * a * c);
    if (discriminant < 0)
        return HitResult.Miss();

    float s = sqrt(discriminant);
    float t = (-b - s) / (2 * a);
    if (t <= tMin || t >= tMax)
        t = (-b + s) / (2 * a);
    if (t <= tMin || t >= tMax)
        return HitResult.Miss();

    float3 p = origin + t * dir;
    float3 n = normalize(p - center);
    float2 uv = float2(
        0.5 + atan2(n.z, n.x) * 0.1591549, // 1/(2π)
        0.5 - n.y * 0.5                    // linear latitude, no asin
    );

    float3 nExt = n;
    if (dot(n, dir) > 0)
        n = -n;

    HitResult result = { t, n, nExt, uv, 0, PrimitiveType.Sphere };
    return result;
}

public HitResult IntersectPlane(float3 origin, float3 dir, Plane p, float tMin, float tMax)
{
    float3 n = normalize(cross(p.U, p.V));
    float denom = dot(n, dir);
    if (abs(denom) < 1e-6)
        return HitResult.Miss();

    float t = dot(n, p.Center - origin) / denom;
    if (t <= tMin || t >= tMax)
        return HitResult.Miss();

    float3 w = origin + t * dir - p.Center;
    float a = dot(w, p.U) / dot(p.U, p.U);
    float b = dot(w, p.V) / dot(p.V, p.V);
    if (max(abs(a), abs(b)) > 1.0)
        return HitResult.Miss();

    float2 uv = float2(a, b) * 0.5 + 0.5; // [-1, 1] to [0, 1]
    float3 nExt = n;
    if (dot(n, dir) > 0)
        n = -n;

    HitResult hit = { t, n, nExt, uv, 0, PrimitiveType.Plane };
    return hit;
}

// Möller–Trumbore. Two-sided, no backface culling: the normal is flipped to face the ray
// for shading while NormalExt keeps the geometric winding for the emission test.
public HitResult IntersectTriangle(float3 origin, float3 dir, Triangle tri, float tMin, float tMax)
{
    float3 e1 = tri.V1 - tri.V0;
    float3 e2 = tri.V2 - tri.V0;
    float3 p = cross(dir, e2);
    float det = dot(e1, p);

    if (abs(det) < 1e-8)
        return HitResult.Miss();

    float invDet = 1.0 / det;
    float3 t = origin - tri.V0;
    float u = dot(t, p) * invDet;
    if (u < 0.0 || u > 1.0)
        return HitResult.Miss();

    float3 q = cross(t, e1);
    float v = dot(dir, q) * invDet;
    if (v < 0.0 || u + v > 1.0)
        return HitResult.Miss();

    float hitT = dot(e2, q) * invDet;
    if (hitT <= tMin || hitT >= tMax)
        return HitResult.Miss();

    float3 normalExt = normalize(cross(e1, e2));
    if (all(normalExt == 0.0))
        return HitResult.Miss();

    float3 normal = normalExt;
    if (dot(normal, dir) > 0)
        normal = -normal;

    HitResult hit = { hitT, normal, normalExt, float2(u, v), 0, PrimitiveType.Triangle };
    return hit;
}

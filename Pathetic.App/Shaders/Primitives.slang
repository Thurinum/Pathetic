module Primitives;

public enum PrimitiveType {
    Plane = 0u,
    Sphere = 1u,
    Triangle = 2u,
    None = 1000u,
}

public struct Sphere
{
    public float3 Center;
    public float Radius;
    public uint MaterialId;
}

public struct Plane
{
    public float3 Center;
    public float3 U;
    public float3 V;
    public uint MaterialId;
}

public struct Triangle
{
    public float3 V0;
    public float3 V1;
    public float3 V2;
    public uint MaterialId;
}

// Unpacks a packed RgbaByte into normalized 0..1 components.
public float3 UnpackRgb(uint packed)
{
    return float3(float(packed & 0xFFu), float((packed >> 8) & 0xFFu), float((packed >> 16) & 0xFFu)) / 255.0f;
}

public struct HitResult 
{
    public float T;
    public float3 Normal;
    public float3 NormalExt;
    public float2 UV;
    public uint Index;
    public uint MaterialId;
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
        h.MaterialId = 0;
        h.Primitive = PrimitiveType.None;
        return h;
    }
}

public HitResult IntersectSphere(float3 origin, float3 dir, Sphere p, float tMin, float tMax) 
{    
    float3 q = origin - p.Center;
    float a = dot(dir,dir);
    float b = 2 * dot(q, dir);
    float c = dot(q,q) - (p.Radius * p.Radius);
    float discriminant = (b * b) - (4 * a * c);
    if (discriminant < 0) 
        return HitResult.Miss();
    
    float s = sqrt(discriminant);
    float t = (-b - s) / (2 * a);
    if (t <= tMin || t >= tMax) 
        t = (-b + s) / (2 * a);
    if (t <= tMin || t >= tMax) 
        return HitResult.Miss();
   
    float3 position = origin + t * dir; // obtenir le point sur la surface de la sphere
    float3 n = normalize(position - p.Center); // la normale est perpendiculaire a la surface
    float2 uv = float2(
        0.5 + atan2(n.z, n.x) * 0.1591549, // 1/(2π)
        0.5 - n.y * 0.5                    // linear latitude, no asin
    );
    
    float3 nExt = n;
    if (dot(n, dir) > 0)
        n = -n;
        
    HitResult result = {};
    result.T = t;
    result.Normal = n;
    result.NormalExt = nExt;
    result.UV = uv;
    result.Index = 0;
    result.MaterialId = p.MaterialId;
    result.Primitive = PrimitiveType.Sphere;
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

    HitResult hit = {};
    hit.T = t;
    hit.Normal = n;
    hit.NormalExt = n;
    hit.UV = uv;
    hit.Index = 0;
    hit.MaterialId = p.MaterialId;
    hit.Primitive = PrimitiveType.Plane;
    return hit;
}

public HitResult IntersectTriangle(float3 origin, float3 dir, Triangle tri, float tMin, float tMax)
{
    float3 edge1 = tri.V1 - tri.V0;
    float3 edge2 = tri.V2 - tri.V0;
    float3 p = cross(dir, edge2);
    float det = dot(edge1, p);
    if (abs(det) < 1e-8)
        return HitResult.Miss();

    float invDet = 1.0 / det;
    float3 s = origin - tri.V0;
    float u = dot(s, p) * invDet;
    if (u < 0.0 || u > 1.0)
        return HitResult.Miss();

    float3 q = cross(s, edge1);
    float v = dot(dir, q) * invDet;
    if (v < 0.0 || u + v > 1.0)
        return HitResult.Miss();

    float t = dot(edge2, q) * invDet;
    if (t <= tMin || t >= tMax)
        return HitResult.Miss();

    float3 n = normalize(cross(edge1, edge2));
    float3 nExt = n;
    if (dot(n, dir) > 0)
        n = -n;

    HitResult result = {};
    result.T = t;
    result.Normal = n;
    result.NormalExt = nExt;
    result.UV = float2(u, v);
    result.Index = 0;
    result.MaterialId = tri.MaterialId;
    result.Primitive = PrimitiveType.Triangle;
    return result;
}

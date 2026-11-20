module Primitives;

public enum PrimitiveType {
    Plane = 0u,
    Sphere = 1u,
    Circle = 2u,
}

public struct Primitive
{
    public PrimitiveType Type;
    public float3 Center;
    public uint MaterialId;
    public uint DataIdx;
    public float3 Min;
    public float3 Max;
}

public struct SphereData
{
    public float Radius;
}

public struct PlaneData
{
    public float3 HalfExtentX;
    public float3 HalfExtentY;
}

StructuredBuffer<SphereData, ScalarDataLayout> sphereData;
StructuredBuffer<PlaneData, ScalarDataLayout> planeData;

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
    
    public bool IsHit() { return T > 0; }
    
    public static HitResult Miss() 
    {
        HitResult h;
        h.T = -1;
        h.Normal = 0;
        h.NormalExt = 0;
        h.UV = 0;
        h.Index = 0;
        return h;
    }
}

public HitResult IntersectSphere(float3 origin, float3 dir, Primitive p, SphereData d, float tMin, float tMax) 
{    
    float3 q = origin - p.Center;
    float a = dot(dir,dir);
    float b = 2 * dot(q, dir);
    float c = dot(q,q) - (d.Radius * d.Radius);
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
        
    HitResult result = { t, n, nExt, uv, 0 };
    return result; 
}

public HitResult IntersectPlane(float3 origin, float3 dir, Primitive p, PlaneData d, float tMin, float tMax)
{
    float3 n = normalize(cross(d.HalfExtentX, d.HalfExtentY));
    float denom = dot(n, dir);
    if (abs(denom) < 1e-6) 
        return HitResult.Miss();

    float t = dot(n, p.Center - origin) / denom;
    if (t <= tMin || t >= tMax) 
        return HitResult.Miss();

    float3 w = origin + t * dir - p.Center;
    float a = dot(w, d.HalfExtentX) / dot(d.HalfExtentX, d.HalfExtentX);
    float b = dot(w, d.HalfExtentY) / dot(d.HalfExtentY, d.HalfExtentY);
    if (max(abs(a), abs(b)) > 1.0) 
        return HitResult.Miss();

    float2 uv = float2(a, b) * 0.5 + 0.5; // [-1, 1] to [0, 1]

    HitResult hit = { t, n, n, uv, 0 };
    return hit;
}

public HitResult IntersectPrimitive(float3 origin, float3 dir, Primitive p, float tMin, float tMax)
{
    switch (p.Type)
    {
        case PrimitiveType.Plane:
            return IntersectPlane(origin, dir, p, planeData[p.DataIdx], tMin, tMax);
        case PrimitiveType.Sphere:
            return IntersectSphere(origin, dir, p, sphereData[p.DataIdx], tMin, tMax);
        default:
            return HitResult.Miss();
    }
}

module Primitives;

public struct Sphere
{
    public float3 Center;
    public float Radius;
    public float3 Albedo;
    public float3 Emission;
}

public struct Plane 
{
    public float3 Center;
    public float3 U;
    public float3 V;
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

public HitResult IntersectSphere(float3 origin, float3 dir, float3 center, float radius, float tMin, float tMax) 
{    
    float3 q = origin - center;
    float a = dot(dir,dir);
    float b = 2 * dot(q, dir);
    float c = dot(q,q) - (radius * radius);
    float discriminant = (b * b) - (4 * a * c);
    if (discriminant < 0) 
        return HitResult.Miss();
    
    float s = sqrt(discriminant);
    float t = (-b - s) / (2 * a);
    if (t <= tMin || t >= tMax) 
        t = (-b + s) / (2 * a);
    if (t <= tMin || t >= tMax) 
        return HitResult.Miss();
   
    const float PI = 3.14159265;
    float3 p = origin + t * dir; // obtenir le point sur la surface de la sphere
    float3 n = normalize(p - center); // la normale est perpendiculaire a la surface
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

    HitResult hit = { t, n, n, uv, 0 };
    return hit;
}
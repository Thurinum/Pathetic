module Camera;

import Random;

static const float Pi = 3.14159265358979323846;

public struct Camera
{
    public float3 Origin;    
    public float _Pad;
    public float3 Up;        
    public float TanHalfFov;
    public float3 Right;     
    float FocusDistance;
    public float3 Forward;   
    float ApertureRadius;
};
       
public struct Ray
{
    public float3 Origin;
    public float3 Direction;
}

public Ray BuildPrimaryRay(Camera camera, inout Rng rng, uint2 pixel, uint2 frameSize, float2 jitter)
{
    float aspect = frameSize.x / (float)frameSize.y;
    float2 screenUV  = (float2(pixel) + jitter) / float2(frameSize);
    float viewPlaneX = (2.0 * screenUV.x - 1.0) * aspect * camera.TanHalfFov;
    float viewPlaneY = (1.0 - 2.0 * screenUV.y) * camera.TanHalfFov;
    
    // camera space -> world space (lineair combination, orthonormal basis)
    float3 eyeToImagePlane = camera.Forward + viewPlaneX * camera.Right + viewPlaneY * camera.Up;
    
    // le thin layer dof
    float3 focusPoint = camera.Origin + camera.FocusDistance * eyeToImagePlane;
    float2 apertureSample = SampleAperture(camera, rng);
    float3 rayOrigin = camera.Origin + apertureSample.x * camera.Right + apertureSample.y * camera.Up;
    float3 rayDir = normalize(focusPoint - rayOrigin);
    
    Ray ray;
    ray.Origin = rayOrigin;
    ray.Direction = rayDir;
    return ray;
}

float2 SampleAperture(Camera camera, inout Rng rng)
{
    float angle = 2.0 * Pi * rng.Next();
    float radius = sqrt(rng.Next()); // pour uniformite dans l'aire du cercle
    return float2(radius * cos(angle), radius * sin(angle)) * camera.ApertureRadius;
}
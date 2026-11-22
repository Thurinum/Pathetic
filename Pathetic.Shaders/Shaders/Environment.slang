module Environment;

public float3 Sky(float3 dir) 
{
     float height = (dir.y + 1.0) * 0.5;
     return lerp(float3(1.0, 1.0, 1.0), float3(0.5, 0.7, 1.0), height);
}
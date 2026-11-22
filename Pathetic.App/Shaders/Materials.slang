module Materials;

public struct RgbaByte
{
    public uint Packed;

    public float3 UnpackRgb()
    {
        return float3(float(Packed & 0xFFu), float((Packed >> 8) & 0xFFu), float((Packed >> 16) & 0xFFu)) / 255.0f;
    }
}

public struct Material
{
    public RgbaByte Albedo;
    public RgbaByte Emission;
    public float EmissionStrength;
}

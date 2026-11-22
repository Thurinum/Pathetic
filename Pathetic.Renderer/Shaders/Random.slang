// PCG hash-based random number generator.
// Source: "Hash Functions for GPU Rendering" (Jarzynski & Olano, JCGT 2020)
// https://jcgt.org/published/0009/03/02/
// https://www.reedbeta.com/blog/hash-functions-for-gpu-rendering/

module Random;

public uint PcgHash(uint v)
{
    uint state = v * 747796405u + 2891336453u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return (word >> 22u) ^ word;
}

public struct Rng
{
    public uint State;

    public __init(uint2 pixel, uint frame)
    {
        State = PcgHash(pixel.x + PcgHash(pixel.y + PcgHash(frame)));
    }

    [mutating] public float Next()
    {
        State = PcgHash(State);
        return float(State >> 8) * (1.0 / 16777216.0);
    }
}
void BuildOrthonormalBasis(float3 normal, out float3 tangent, out float3 bitangent)
{
    float zSign  = normal.z >= 0.0 ? 1.0 : -1.0;
    float scale = -1.0 / (zSign  + normal.z);
    float xyTerm = normal.x * normal.y * scale;
    tangent = float3(1.0 + zSign  * normal.x * normal.x * scale, zSign  * xyTerm, -zSign * normal.x);
    bitangent = float3(xyTerm, zSign  + normal.y * normal.y * scale, -normal.y);
}

public float3 RandomBounceDirection(float3 normal, inout Rng rng)
{
    float radiusRandom = rng.Next();
    float angleRandom = rng.Next();

    float radius = sqrt(radiusRandom);
    float angle = 2.0 * float.getPi() * angleRandom;
    float height = sqrt(max(0.0, 1.0 - radiusRandom));

    float3 tangent, bitangent;
    BuildOrthonormalBasis(normal, tangent, bitangent);
    return tangent * (radius * cos(angle)) + bitangent * (radius * sin(angle)) + normal * height;
}

public float3 RandomUniformHemisphereDirection(float3 normal, inout Rng rng)
{
    float heightRandom = rng.Next();
    float angleRandom = rng.Next();

    float height = heightRandom;
    float radius = sqrt(max(0.0, 1.0 - height * height));
    float angle = 2.0 * float.getPi() * angleRandom;

    float3 tangent, bitangent;
    BuildOrthonormalBasis(normal, tangent, bitangent);
    return tangent * (radius * cos(angle)) + bitangent * (radius * sin(angle)) + normal * height;
}

public float2 StratifiedPoint(uint cellIndex, uint cellsPerAxis, inout Rng rng)
{
    float2 cell = float2(cellIndex % cellsPerAxis, cellIndex / cellsPerAxis);
    return (cell + float2(rng.Next(), rng.Next())) / cellsPerAxis;
}

public void ShuffleCells(out uint cells[64], uint count, inout Rng rng)
{
    for (uint index = 0; index < count; ++index)
        cells[index] = index;
    for (uint last = count - 1; last > 0; --last)
    {
        uint other = min((uint)(rng.Next() * (last + 1)), last);
        uint swapped = cells[last];
        cells[last] = cells[other];
        cells[other] = swapped;
    }
}

using System.Numerics;
using System.Runtime.InteropServices;

namespace Pathetic.App.RenderPasses;

/// <summary>Mirrors the constant buffer in PrimaryRay.slang.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct PrimaryRayParams
{
    public Vector3 Origin;    // 0
    public float _Pad0;       // 12
    public Vector3 Direction; // 16
    public float _Pad1;       // 28
    public uint PlaneCount;   // 32
    public uint SphereCount;  // 36
}

/// <summary>Mirrors the RWStructuredBuffer element in PrimaryRay.slang.</summary>
[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct HitResult
{
    public float T;
    public uint Primitive;
    public uint Index;
    public uint _Pad;

    public readonly bool IsHit => T < 1e29f;
}

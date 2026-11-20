using System.Numerics;
using System.Runtime.InteropServices;

namespace Pathetic.App.Components;

public enum PrimitiveType : uint
{
    Plane = 0,
    Sphere = 1,
    Circle = 2
}

[StructLayout(LayoutKind.Explicit, Size = 48)]
public record struct Primitive
{
    [FieldOffset(00)] public PrimitiveType Type;
    [FieldOffset(04)] public Vector3 Center;
    [FieldOffset(16)] public uint MaterialId;
    [FieldOffset(20)] public uint DataIdx;
    [FieldOffset(24)] public Vector3 Min;
    [FieldOffset(36)] public Vector3 Max;
}

[StructLayout(LayoutKind.Sequential)]
public record struct SphereData
{
    public float Radius;
}

[StructLayout(LayoutKind.Sequential)]
public record struct PlaneData
{
    public Vector3 HalfExtentX;
    public Vector3 HalfExtentY;
}

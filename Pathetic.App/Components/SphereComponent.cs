using System.Numerics;
using System.Runtime.InteropServices;
using NeoVeldrid;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

[StructLayout(LayoutKind.Explicit, Size = 32)]
public record struct SpherePrimitive
{
    [FieldOffset(00)] public Vector3 Center;
    [FieldOffset(12)] public float Radius;
    [FieldOffset(16)] public RgbaByte Albedo;
    [FieldOffset(20)] public RgbaByte Emission;
    [FieldOffset(24)] public uint ObjectId; // owning SceneObject id, used by the selection passes
}

public class SphereComponent(SparseSet<SpherePrimitive> primitives) : RendererBase<SpherePrimitive>(primitives)
{
    public float Radius { get; set; } = 1.0f;
    public RgbaByte Albedo { get; set; } = new(128, 128, 128, 255);
    public RgbaByte Emission { get; set; }

    protected override SpherePrimitive BuildPrimitive()
    {
        return new SpherePrimitive
        {
            Center = Parent.Transform.Position,
            Radius = Radius,
            Albedo = Albedo,
            Emission = Emission,
            ObjectId = Parent.Id
        };
    }
}

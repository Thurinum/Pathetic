using System.Numerics;
using System.Runtime.InteropServices;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

[StructLayout(LayoutKind.Explicit, Size = 20)]
public record struct SpherePrimitive
{
    [FieldOffset(00)] public Vector3 Center;
    [FieldOffset(12)] public float Radius;
    [FieldOffset(16)] public uint MaterialId;
}

public class SphereComponent(SparseSet<SpherePrimitive> primitives, MaterialLibrary materials) : RendererBase<SpherePrimitive>(primitives)
{
    public float Radius { get; set; } = 1.0f;
    public Material Material { get; set; } = Material.Default;

    protected override SpherePrimitive BuildPrimitive()
    {
        return new SpherePrimitive
        {
            Center = Parent.Transform.Position,
            Radius = Radius,
            MaterialId = materials.GetOrAdd(Material)
        };
    }
}

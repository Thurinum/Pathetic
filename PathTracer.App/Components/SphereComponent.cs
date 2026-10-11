using System.Numerics;
using System.Runtime.InteropServices;
using PathTracerCore.Bvh;
using PathTracerCore.Renderer;
using PathTracerCore.SceneGraph;

namespace PathTracerApp.Components;

[StructLayout(LayoutKind.Explicit, Size = 40)]
public record struct SpherePrimitive : IBvhReady
{
    [FieldOffset(00)] public Vector3 Center;
    [FieldOffset(12)] public float Radius;
    [FieldOffset(16)] public Color Albedo;
    [FieldOffset(28)] public Color Emission;
    
    public uint BvhType => 1;
    
    public AxisAlignedBoundingBox BvhBounds => new AxisAlignedBoundingBox
    {
        Min = Center - new Vector3(Radius),
        Max = Center + new Vector3(Radius)
    };
}

public class SphereComponent : RendererBase<SpherePrimitive>
{
    public float Radius { get; set; } = 1.0f;
    public Color Albedo { get; set; } = Color.Red;
    public Color Emission { get; set; } = new(0.5f, 0.5f, 0.5f);

    protected override SpherePrimitive BuildPrimitive()
    {
        return new SpherePrimitive
        {
            Center = Parent.Transform.Position,
            Radius = Radius,
            Albedo = Albedo,
            Emission = Emission
        };
    }
}
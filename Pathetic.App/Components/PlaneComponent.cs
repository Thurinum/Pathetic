using System.Numerics;
using System.Runtime.InteropServices;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

[StructLayout(LayoutKind.Explicit, Size = 48)]
public record struct PlanePrimitive
{
    [FieldOffset(00)] public Vector3 Point;
    [FieldOffset(16)] public Vector3 ExtentX;
    [FieldOffset(32)] public Vector3 ExtentY;
}   

public class PlaneComponent(SparseSet<PlanePrimitive> primitives) : RendererBase<PlanePrimitive>(primitives)
{
    private Transform _transform = null!;

    public float Width { get; set; } = 5.0f;
    public float Height { get; set; } = 5.0f;

    public override void Awake()
    {
        _transform = Parent.GetComponent<Transform>();
        base.Awake();
    }

    protected override PlanePrimitive BuildPrimitive()
    {
        return new PlanePrimitive
        {
            Point = _transform.Position,
            ExtentX = Vector3.Transform(Vector3.UnitX, _transform.Rotation) * (Width * 0.5f),
            ExtentY = Vector3.Transform(Vector3.UnitY, _transform.Rotation) * (Height * 0.5f),

        };
    }
}
using System.Numerics;
using System.Runtime.InteropServices;
using PathTracerCore.Bvh;
using PathTracerCore.SceneGraph;

namespace PathTracerApp.Components;

[StructLayout(LayoutKind.Explicit, Size = 36)]
public record struct PlanePrimitive : IBvhReady
{
    [FieldOffset(00)] public Vector3 Point;
    [FieldOffset(12)] public Vector3 ExtentX;
    [FieldOffset(24)] public Vector3 ExtentY;

    public uint BvhType => 0;

    public AxisAlignedBoundingBox BvhBounds
    {
        get
        {
            var p0 = Point - ExtentX - ExtentY;
            var p1 = Point + ExtentX - ExtentY;
            var p2 = Point - ExtentX + ExtentY;
            var p3 = Point + ExtentX + ExtentY;

            return AxisAlignedBoundingBox.Empty
                .Include(p0).Include(p1).Include(p2).Include(p3);
        }
    }
}

public class PlaneComponent : RendererBase<PlanePrimitive>
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
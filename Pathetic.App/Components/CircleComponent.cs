using System.Numerics;
using System.Runtime.InteropServices;
using Pathetic.Renderer.Buffers;
using Pathetic.Renderer.Utils;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

[StructLayout(LayoutKind.Sequential)]
public record struct CirclePrimitive
{
    public Vector2 Position01;
    public float Radius;
    public float Pad;
}

public class CircleComponent(SparseSet<CirclePrimitive> primitives) : RendererBase<CirclePrimitive>(primitives)
{
    public float Radius { get; set; }
    private Transform _transform = null!;

    protected override void Setup()
    {
        _transform = Parent.GetComponent<Transform>();
        base.Setup();
    }

    protected override CirclePrimitive BuildPrimitive()
    {
        return new CirclePrimitive
        {
            Position01 = _transform.Position.XY,
            Radius = Radius
        };
    }
}
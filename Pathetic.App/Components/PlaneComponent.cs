using System.Numerics;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

public class PlaneComponent(SparseSet<GPUTypes.Plane> primitives, MaterialLibrary materials) : RendererBase<GPUTypes.Plane>(primitives)
{
    private Transform _transform = null!;

    public float Width { get; set; } = 5.0f;
    public float Height { get; set; } = 5.0f;
    public Material Material { get; set; } = Material.Default;

    protected override void Setup()
    {
        _transform = Parent.GetComponent<Transform>();
        base.Setup();
    }

    protected override GPUTypes.Plane BuildPrimitive()
    {
        return new GPUTypes.Plane
        {
            Center = _transform.Position,
            U = Vector3.Transform(Vector3.UnitX, _transform.Rotation) * (Width * 0.5f),
            V = Vector3.Transform(Vector3.UnitY, _transform.Rotation) * (Height * 0.5f),
            MaterialId = materials.GetOrAdd(Material)
        };
    }
}

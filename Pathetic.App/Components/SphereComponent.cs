using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

public class SphereComponent(SparseSet<GPUType.Sphere> primitives, MaterialLibrary materials) : RendererBase<GPUType.Sphere>(primitives)
{
    public float Radius { get; set; } = 1.0f;
    public Material Material { get; set; } = Material.Default;

    protected override GPUType.Sphere BuildPrimitive()
    {
        return new GPUType.Sphere
        {
            Center = Parent.Transform.Position,
            Radius = Radius,
            MaterialId = materials.GetOrAdd(Material)
        };
    }
}

using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

public class SphereComponent(SparseSet<Gpu.Sphere> primitives, MaterialLibrary materials) : RendererBase<Gpu.Sphere>(primitives)
{
    public float Radius { get; set; } = 1.0f;
    public Material Material { get; set; } = Material.Default;

    protected override Gpu.Sphere BuildPrimitive()
    {
        return new Gpu.Sphere
        {
            Center = Parent.Transform.Position,
            Radius = Radius,
            MaterialId = materials.GetOrAdd(Material)
        };
    }
}

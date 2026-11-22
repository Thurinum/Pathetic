using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

public class SphereComponent(SparseSet<GPUType.Primitives.Sphere> primitives, MaterialLibrary materials) : RendererBase<GPUType.Primitives.Sphere>(primitives)
{
    public float Radius { get; set; } = 1.0f;
    public Material Material { get; set; } = Material.Default;

    protected override GPUType.Primitives.Sphere BuildPrimitive()
    {
        return new GPUType.Primitives.Sphere
        {
            Center = Parent.Transform.Position,
            Radius = Radius,
            MaterialId = materials.GetOrAdd(Material)
        };
    }
}

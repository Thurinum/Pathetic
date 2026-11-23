using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

public class SphereComponent(SparseSet<GPUTypes.Sphere> primitives, MaterialLibrary materials) : RendererBase<GPUTypes.Sphere>(primitives)
{
    public float Radius { get; set; } = 1.0f;
    public Material Material { get; set; } = Material.Default;

    protected override GPUTypes.Sphere BuildPrimitive()
    {
        return new GPUTypes.Sphere
        {
            Center = Parent.Transform.Position,
            Radius = Radius,
            MaterialId = materials.GetOrAdd(Material)
        };
    }
}

using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;

namespace Pathetic.App.Components;

public class SphereComponent(SparseSet<SlangGen.Primitives.Sphere> primitives, MaterialLibrary materials) : RendererBase<SlangGen.Primitives.Sphere>(primitives)
{
    public float Radius { get; set; } = 1.0f;
    public Material Material { get; set; } = Material.Default;

    protected override SlangGen.Primitives.Sphere BuildPrimitive()
    {
        return new SlangGen.Primitives.Sphere
        {
            Center = Parent.Transform.Position,
            Radius = Radius,
            MaterialId = materials.GetOrAdd(Material)
        };
    }
}

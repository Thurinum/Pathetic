using System.Numerics;
using Pathetic.Renderer.Buffers;
using Pathetic.Renderer.Utils;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components;

public class CircleComponent(
    SparseSet<Primitive> primitives,
    GpuPool<SphereData> spheres,
    MaterialLibrary materials) : Component
{
    private Handle _handle = Handle.Invalid;
    private uint _dataIdx = GpuPool<SphereData>.InvalidIndex;

    public float Radius { get; set; }
    public Material Material { get; set; } = Material.Default;

    protected override void Setup()
    {
        SphereData data = BuildPayload();
        _dataIdx = spheres.Add(data);
        _handle = primitives.Add(BuildPrimitive(data));
    }

    protected override void LateUpdate(float deltaTime)
    {
        if (!_handle.IsValid)
            return;

        SphereData data = BuildPayload();
        spheres.Update(_dataIdx, data);
        primitives.Update(_handle, BuildPrimitive(data));
    }

    protected override void Cleanup()
    {
        if (_handle.IsValid)
            primitives.Remove(ref _handle);

        if (_dataIdx == GpuPool<SphereData>.InvalidIndex)
            return;

        spheres.Remove(_dataIdx);
        _dataIdx = GpuPool<SphereData>.InvalidIndex;
    }

    private SphereData BuildPayload() => new() { Radius = Radius };

    private Primitive BuildPrimitive(SphereData data)
    {
        Vector3 center = new(Parent.Transform.Position.XY, 0.0f);
        Vector3 extent = new(data.Radius);

        return new Primitive
        {
            Type = PrimitiveType.Circle,
            Center = center,
            MaterialId = materials.GetOrAdd(Material),
            DataIdx = _dataIdx,
            Min = center - extent,
            Max = center + extent
        };
    }
}

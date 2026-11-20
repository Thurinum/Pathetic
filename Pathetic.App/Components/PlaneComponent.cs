using System.Numerics;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components;

public class PlaneComponent(
    SparseSet<Primitive> primitives,
    GpuPool<PlaneData> planes,
    MaterialLibrary materials) : Component
{
    private Handle _handle = Handle.Invalid;
    private uint _dataIdx = GpuPool<PlaneData>.InvalidIndex;

    public float Width { get; set; } = 5.0f;
    public float Height { get; set; } = 5.0f;
    public Material Material { get; set; } = Material.Default;

    protected override void Setup()
    {
        PlaneData data = BuildPayload();
        _dataIdx = planes.Add(data);
        _handle = primitives.Add(BuildPrimitive(data));
    }

    protected override void LateUpdate(float deltaTime)
    {
        if (!_handle.IsValid)
            return;

        PlaneData data = BuildPayload();
        planes.Update(_dataIdx, data);
        primitives.Update(_handle, BuildPrimitive(data));
    }

    protected override void Cleanup()
    {
        if (_handle.IsValid)
            primitives.Remove(ref _handle);

        if (_dataIdx == GpuPool<PlaneData>.InvalidIndex)
            return;

        planes.Remove(_dataIdx);
        _dataIdx = GpuPool<PlaneData>.InvalidIndex;
    }

    private PlaneData BuildPayload()
    {
        Quaternion rotation = Parent.Transform.Rotation;

        return new PlaneData
        {
            HalfExtentX = Vector3.Transform(Vector3.UnitX, rotation) * (Width * 0.5f),
            HalfExtentY = Vector3.Transform(Vector3.UnitY, rotation) * (Height * 0.5f)
        };
    }

    private Primitive BuildPrimitive(PlaneData data)
    {
        Vector3 center = Parent.Transform.Position;
        Vector3 extent = Vector3.Abs(data.HalfExtentX) + Vector3.Abs(data.HalfExtentY);

        return new Primitive
        {
            Type = PrimitiveType.Plane,
            Center = center,
            MaterialId = materials.GetOrAdd(Material),
            DataIdx = _dataIdx,
            Min = center - extent,
            Max = center + extent
        };
    }
}

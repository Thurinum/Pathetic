using System.Numerics;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components;

public class MeshComponent(
    SparseSet<Primitive> primitives,
    GpuPool<MeshData> meshes,
    StructuredBuffer<MeshTriangle> triangles,
    MaterialLibrary materials) : Component
{
    private Handle _handle = Handle.Invalid;
    private uint _dataIdx = GpuPool<MeshData>.InvalidIndex;
    private uint _startTriangleIdx;
    private int _triangleCount;
    private Vector3 _bakedPosition;
    private Quaternion _bakedRotation = Quaternion.Identity;
    private Vector3 _bakedScale = Vector3.One;

    public MeshTriangle[] Triangles { get; set; } = [];
    public Material Material { get; set; } = Material.Default;

    protected override void Setup()
    {
        _handle = primitives.Add(BuildPrimitive(append: true));
    }

    protected override void LateUpdate(float deltaTime)
    {
        if (!_handle.IsValid)
            return;

        bool reallocate = Triangles.Length != _triangleCount;
        if (!reallocate && !TransformChanged())
            return;

        primitives.Update(_handle, BuildPrimitive(reallocate));
    }

    protected override void Cleanup()
    {
        if (_handle.IsValid)
            primitives.Remove(ref _handle);

        if (_dataIdx == GpuPool<MeshData>.InvalidIndex)
            return;

        meshes.Remove(_dataIdx);
        _dataIdx = GpuPool<MeshData>.InvalidIndex;
    }

    private Primitive BuildPrimitive(bool append)
    {
        Transform transform = Parent.Transform;
        Matrix4x4 matrix = Matrix4x4.CreateScale(transform.Scale)
            * Matrix4x4.CreateFromQuaternion(transform.Rotation)
            * Matrix4x4.CreateTranslation(transform.Position);

        Vector3 min = new(float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity);

        if (append)
            _startTriangleIdx = (uint)triangles.Count;

        for (int i = 0; i < Triangles.Length; i++)
        {
            MeshTriangle triangle = TransformTriangle(Triangles[i], matrix);

            if (append)
                triangles.Add(triangle);
            else
                triangles.UpdateAt((int)(_startTriangleIdx + i), triangle);

            min = Vector3.Min(min, Vector3.Min(triangle.V0, Vector3.Min(triangle.V1, triangle.V2)));
            max = Vector3.Max(max, Vector3.Max(triangle.V0, Vector3.Max(triangle.V1, triangle.V2)));
        }

        _triangleCount = Triangles.Length;

        if (append)
        {
            MeshData data = new()
            {
                StartTriangleIdx = _startTriangleIdx,
                TriangleCount = (uint)_triangleCount
            };

            if (_dataIdx == GpuPool<MeshData>.InvalidIndex)
                _dataIdx = meshes.Add(data);
            else
                meshes.Update(_dataIdx, data);
        }

        if (!float.IsFinite(min.X))
            min = max = transform.Position;

        _bakedPosition = transform.Position;
        _bakedRotation = transform.Rotation;
        _bakedScale = transform.Scale;

        return new Primitive
        {
            Type = PrimitiveType.Mesh,
            Center = (min + max) * 0.5f,
            MaterialId = materials.GetOrAdd(Material),
            DataIdx = _dataIdx,
            Min = min,
            Max = max
        };
    }

    private bool TransformChanged()
    {
        Transform transform = Parent.Transform;
        return transform.Position != _bakedPosition
            || transform.Rotation != _bakedRotation
            || transform.Scale != _bakedScale;
    }

    private static MeshTriangle TransformTriangle(MeshTriangle triangle, Matrix4x4 matrix) => new()
    {
        V0 = Vector3.Transform(triangle.V0, matrix),
        V1 = Vector3.Transform(triangle.V1, matrix),
        V2 = Vector3.Transform(triangle.V2, matrix)
    };
}

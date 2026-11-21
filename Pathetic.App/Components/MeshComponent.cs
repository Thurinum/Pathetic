using System.Numerics;
using System.Runtime.InteropServices;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components;

[StructLayout(LayoutKind.Explicit, Size = 80)]
public record struct TrianglePrimitive
{
    [FieldOffset(00)] public Vector3 V0;
    [FieldOffset(12)] public Vector3 V1;
    [FieldOffset(24)] public Vector3 V2;
    [FieldOffset(36)] public Vector3 N0;
    [FieldOffset(48)] public Vector3 N1;
    [FieldOffset(60)] public Vector3 N2;
    [FieldOffset(72)] public uint MaterialId;
    [FieldOffset(76)] public uint TransformId;
}

public record struct MeshTriangle(Vector3 V0, Vector3 V1, Vector3 V2, Vector3 N0, Vector3 N1, Vector3 N2);

public class MeshComponent(
    SparseSet<TrianglePrimitive> triangles,
    MaterialLibrary materials,
    MeshTransformLibrary transforms) : Component
{
    private readonly List<Handle> _handles = [];
    private Vector3 _bakedPosition;
    private Quaternion _bakedRotation = Quaternion.Identity;
    private Vector3 _bakedScale = Vector3.One;
    private uint _transformId = MeshTransform.None;

    public MeshTriangle[] Triangles { get; set; } = [];
    public Material Material { get; set; } = Material.Default;

    public bool KeepLocalGeometry { get; set; }

    protected override void Setup()
    {
        Rebuild();
        BakeTransform();
    }

    protected override void LateUpdate(float deltaTime)
    {
        if (Triangles.Length == _handles.Count && !TransformChanged())
            return;

        Rebuild();
        BakeTransform();
    }

    protected override void Cleanup()
    {
        RemoveTriangles();

        if (_transformId == MeshTransform.None)
            return;

        transforms.Remove(_transformId);
        _transformId = MeshTransform.None;
    }

    private void RemoveTriangles()
    {
        for (int i = _handles.Count - 1; i >= 0; i--)
        {
            Handle handle = _handles[i];
            triangles.Remove(ref handle);
        }

        _handles.Clear();
    }

    private void Rebuild()
    {
        uint materialId = materials.GetOrAdd(Material);
        Matrix4x4 matrix = BuildMatrix();
        bool local = KeepLocalGeometry;
        uint transformId = MeshTransform.None;

        if (local)
        {
            MeshTransform transform = MeshTransform.FromMatrix(matrix);

            if (_transformId == MeshTransform.None)
                _transformId = transforms.Add(transform);
            else
                transforms.Update(_transformId, transform);

            transformId = _transformId;
        }
        else if (_transformId != MeshTransform.None)
        {
            transforms.Remove(_transformId);
            _transformId = MeshTransform.None;
        }

        Matrix4x4 vertexMatrix = local ? Matrix4x4.Identity : matrix;
        Matrix4x4 normalMatrix = Matrix4x4.Identity;

        if (!local && Matrix4x4.Invert(matrix, out Matrix4x4 inverse))
            normalMatrix = Matrix4x4.Transpose(inverse);

        if (_handles.Count != Triangles.Length)
        {
            RemoveTriangles();

            foreach (MeshTriangle triangle in Triangles)
                _handles.Add(triangles.Add(ToPrimitive(triangle, vertexMatrix, normalMatrix, materialId, transformId)));

            return;
        }

        for (int i = 0; i < Triangles.Length; i++)
            triangles.Update(_handles[i], ToPrimitive(Triangles[i], vertexMatrix, normalMatrix, materialId, transformId));
    }

    private Matrix4x4 BuildMatrix()
    {
        Transform transform = Parent.Transform;
        return Matrix4x4.CreateScale(transform.Scale)
            * Matrix4x4.CreateFromQuaternion(transform.Rotation)
            * Matrix4x4.CreateTranslation(transform.Position);
    }

    private static TrianglePrimitive ToPrimitive(
        MeshTriangle triangle,
        Matrix4x4 matrix,
        Matrix4x4 normalMatrix,
        uint materialId,
        uint transformId) => new()
    {
        V0 = Vector3.Transform(triangle.V0, matrix),
        V1 = Vector3.Transform(triangle.V1, matrix),
        V2 = Vector3.Transform(triangle.V2, matrix),
        N0 = TransformNormal(triangle.N0, normalMatrix),
        N1 = TransformNormal(triangle.N1, normalMatrix),
        N2 = TransformNormal(triangle.N2, normalMatrix),
        MaterialId = materialId,
        TransformId = transformId
    };

    private static Vector3 TransformNormal(Vector3 normal, Matrix4x4 normalMatrix)
    {
        Vector3 transformed = Vector3.TransformNormal(normal, normalMatrix);
        float length = transformed.Length();
        return length > 1e-12f ? transformed / length : Vector3.Zero;
    }

    private bool TransformChanged()
    {
        Transform transform = Parent.Transform;
        return transform.Position != _bakedPosition
            || transform.Rotation != _bakedRotation
            || transform.Scale != _bakedScale;
    }

    private void BakeTransform()
    {
        Transform transform = Parent.Transform;
        _bakedPosition = transform.Position;
        _bakedRotation = transform.Rotation;
        _bakedScale = transform.Scale;
    }
}

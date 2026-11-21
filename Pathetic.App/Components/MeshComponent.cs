using System.Numerics;
using System.Runtime.InteropServices;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components;

[StructLayout(LayoutKind.Explicit, Size = 76)]
public record struct TrianglePrimitive
{
    [FieldOffset(00)] public Vector3 V0;
    [FieldOffset(12)] public Vector3 V1;
    [FieldOffset(24)] public Vector3 V2;
    [FieldOffset(36)] public Vector3 N0;
    [FieldOffset(48)] public Vector3 N1;
    [FieldOffset(60)] public Vector3 N2;
    [FieldOffset(72)] public uint MaterialId;
}

public record struct MeshTriangle(Vector3 V0, Vector3 V1, Vector3 V2, Vector3 N0, Vector3 N1, Vector3 N2);

public class MeshComponent(SparseSet<TrianglePrimitive> triangles, MaterialLibrary materials) : Component
{
    private readonly List<Handle> _handles = [];
    private Vector3 _bakedPosition;
    private Quaternion _bakedRotation = Quaternion.Identity;
    private Vector3 _bakedScale = Vector3.One;

    public MeshTriangle[] Triangles { get; set; } = [];
    public Material Material { get; set; } = Material.Default;

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
        Matrix4x4 normalMatrix = Matrix4x4.Invert(matrix, out Matrix4x4 inverse)
            ? Matrix4x4.Transpose(inverse)
            : Matrix4x4.Identity;

        if (_handles.Count != Triangles.Length)
        {
            Cleanup();

            foreach (MeshTriangle triangle in Triangles)
                _handles.Add(triangles.Add(ToPrimitive(triangle, matrix, normalMatrix, materialId)));

            return;
        }

        for (int i = 0; i < Triangles.Length; i++)
            triangles.Update(_handles[i], ToPrimitive(Triangles[i], matrix, normalMatrix, materialId));
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
        uint materialId) => new()
    {
        V0 = Vector3.Transform(triangle.V0, matrix),
        V1 = Vector3.Transform(triangle.V1, matrix),
        V2 = Vector3.Transform(triangle.V2, matrix),
        N0 = TransformNormal(triangle.N0, normalMatrix),
        N1 = TransformNormal(triangle.N1, normalMatrix),
        N2 = TransformNormal(triangle.N2, normalMatrix),
        MaterialId = materialId
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

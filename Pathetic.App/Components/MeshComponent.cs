using System.Numerics;
using System.Runtime.InteropServices;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components;

[StructLayout(LayoutKind.Explicit, Size = 40)]
public record struct TrianglePrimitive
{
    [FieldOffset(00)] public Vector3 V0;
    [FieldOffset(12)] public Vector3 V1;
    [FieldOffset(24)] public Vector3 V2;
    [FieldOffset(36)] public uint MaterialId;
}

public record struct MeshTriangle(Vector3 V0, Vector3 V1, Vector3 V2);

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

        if (_handles.Count != Triangles.Length)
        {
            Cleanup();

            foreach (MeshTriangle triangle in Triangles)
                _handles.Add(triangles.Add(ToPrimitive(triangle, matrix, materialId)));

            return;
        }

        for (int i = 0; i < Triangles.Length; i++)
            triangles.Update(_handles[i], ToPrimitive(Triangles[i], matrix, materialId));
    }

    private Matrix4x4 BuildMatrix()
    {
        Transform transform = Parent.Transform;
        return Matrix4x4.CreateScale(transform.Scale)
            * Matrix4x4.CreateFromQuaternion(transform.Rotation)
            * Matrix4x4.CreateTranslation(transform.Position);
    }

    private static TrianglePrimitive ToPrimitive(MeshTriangle triangle, Matrix4x4 matrix, uint materialId) => new()
    {
        V0 = Vector3.Transform(triangle.V0, matrix),
        V1 = Vector3.Transform(triangle.V1, matrix),
        V2 = Vector3.Transform(triangle.V2, matrix),
        MaterialId = materialId
    };

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

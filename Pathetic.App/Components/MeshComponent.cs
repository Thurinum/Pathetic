using System.Numerics;
using Pathetic.App.Assets;
using Pathetic.Renderer.Buffers;
using Pathetic.SceneGraph;
using Pathetic.SceneGraph.Components;

namespace Pathetic.App.Components;

public record struct MeshTriangle(Vector3 V0, Vector3 V1, Vector3 V2, Vector3 N0, Vector3 N1, Vector3 N2);

public class MeshComponent(
    SparseSet<GPUType.Triangle> triangles,
    MaterialLibrary materials,
    MeshTransformLibrary transforms) : Component
{
    private readonly List<Handle> _handles = [];
    private Vector3 _bakedPosition;
    private Quaternion _bakedRotation = Quaternion.Identity;
    private Vector3 _bakedScale = Vector3.One;
    private uint _transformId = MeshTransformLibrary.None;

    public MeshAsset? Asset { get; set; }

    protected override void Setup()
    {
        if (Asset is null)
            return;

        _transformId = transforms.Add(MeshTransformExtensions.FromMatrix(BuildMatrix()));

        uint materialId = materials.GetOrAdd(Asset.Material);

        foreach (MeshTriangle triangle in Asset.Triangles)
            _handles.Add(triangles.Add(ToPrimitive(triangle, materialId, _transformId)));

        BakeTransform();
    }

    protected override void LateUpdate(float deltaTime)
    {
        if (_transformId == MeshTransformLibrary.None || !TransformChanged())
            return;

        transforms.Update(_transformId, MeshTransformExtensions.FromMatrix(BuildMatrix()));
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

        if (_transformId == MeshTransformLibrary.None)
            return;

        transforms.Remove(_transformId);
        _transformId = MeshTransformLibrary.None;
    }

    private Matrix4x4 BuildMatrix()
    {
        Transform transform = Parent.Transform;
        return Matrix4x4.CreateScale(transform.Scale)
            * Matrix4x4.CreateFromQuaternion(transform.Rotation)
            * Matrix4x4.CreateTranslation(transform.Position);
    }

    private static GPUType.Triangle ToPrimitive(MeshTriangle triangle, uint materialId, uint transformId) => new()
    {
        V0 = triangle.V0,
        V1 = triangle.V1,
        V2 = triangle.V2,
        N0 = triangle.N0,
        N1 = triangle.N1,
        N2 = triangle.N2,
        MaterialId = materialId,
        TransformId = transformId
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

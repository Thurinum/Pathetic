using System.Numerics;
using Pathetic.App.Components;
using Pathetic.Renderer.Bvh;
using Pathetic.Renderer.Buffers;

namespace Pathetic.App.Bvh;

public sealed class SceneBvh(
    StructuredBuffer<GPUTypes.Sphere> spheres,
    StructuredBuffer<GPUTypes.Plane> planes,
    StructuredBuffer<GPUTypes.Triangle> triangles,
    StructuredBuffer<GPUTypes.MeshTransform> meshTransforms)
{
    private const uint PlaneType = 0;
    private const uint SphereType = 1;
    private const uint TriangleType = 2;

    private GPUTypes.BvhNode[] _nodes = [];
    private GPUTypes.BvhPrimitiveRef[] _primitiveRefs = [];
    private uint _sphereRevision;
    private uint _planeRevision;
    private uint _triangleRevision;
    private uint _transformRevision;
    private bool _built;

    public uint Version { get; private set; }
    public GPUTypes.BvhNode[] Nodes => _nodes;
    public GPUTypes.BvhPrimitiveRef[] PrimitiveRefs => _primitiveRefs;

    public bool IsDirty =>
        !_built
        || _sphereRevision != spheres.Revision
        || _planeRevision != planes.Revision
        || _triangleRevision != triangles.Revision
        || _transformRevision != meshTransforms.Revision;

    public void Rebuild()
    {
        List<BvhPrimitiveRef> references = [];
        AppendSpheres(references);
        AppendPlanes(references);
        AppendTriangles(references);

        List<GPUTypes.BvhNode> nodes = [];
        List<GPUTypes.BvhPrimitiveRef> primitiveRefs = [];

        if (references.Count > 0)
        {
            BvhNode root = BvhBuilder.Build(references);
            root.Flatten(nodes, primitiveRefs);
        }

        _nodes = nodes.ToArray();
        _primitiveRefs = primitiveRefs.ToArray();
        _sphereRevision = spheres.Revision;
        _planeRevision = planes.Revision;
        _triangleRevision = triangles.Revision;
        _transformRevision = meshTransforms.Revision;
        _built = true;
        Version++;
    }

    private void AppendSpheres(List<BvhPrimitiveRef> references)
    {
        ReadOnlySpan<GPUTypes.Sphere> source = spheres.Values;
        for (uint i = 0; i < source.Length; i++)
        {
            GPUTypes.Sphere sphere = source[(int)i];
            Vector3 radius = new(sphere.Radius);
            references.Add(new BvhPrimitiveRef(
                SphereType,
                i,
                new AxisAlignedBoundingBox(sphere.Center - radius, sphere.Center + radius)));
        }
    }

    private void AppendPlanes(List<BvhPrimitiveRef> references)
    {
        ReadOnlySpan<GPUTypes.Plane> source = planes.Values;
        for (uint i = 0; i < source.Length; i++)
        {
            GPUTypes.Plane plane = source[(int)i];
            Vector3 p0 = plane.Center - plane.U - plane.V;
            Vector3 p1 = plane.Center + plane.U - plane.V;
            Vector3 p2 = plane.Center - plane.U + plane.V;
            Vector3 p3 = plane.Center + plane.U + plane.V;

            AxisAlignedBoundingBox bounds = AxisAlignedBoundingBox.Empty
                .Include(p0).Include(p1).Include(p2).Include(p3);

            references.Add(new BvhPrimitiveRef(PlaneType, i, bounds));
        }
    }

    private void AppendTriangles(List<BvhPrimitiveRef> references)
    {
        ReadOnlySpan<GPUTypes.Triangle> source = triangles.Values;
        ReadOnlySpan<GPUTypes.MeshTransform> transforms = meshTransforms.Values;

        for (uint i = 0; i < source.Length; i++)
        {
            GPUTypes.Triangle triangle = source[(int)i];

            AxisAlignedBoundingBox bounds = AxisAlignedBoundingBox.Empty
                .Include(triangle.V0).Include(triangle.V1).Include(triangle.V2);

            if (triangle.TransformId < transforms.Length)
                bounds = TransformBounds(bounds, transforms[(int)triangle.TransformId]);

            references.Add(new BvhPrimitiveRef(TriangleType, i, bounds));
        }
    }

    private static AxisAlignedBoundingBox TransformBounds(AxisAlignedBoundingBox bounds, GPUTypes.MeshTransform transform)
    {
        AxisAlignedBoundingBox result = AxisAlignedBoundingBox.Empty;

        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 point = new(
                (corner & 1) == 0 ? bounds.Min.X : bounds.Max.X,
                (corner & 2) == 0 ? bounds.Min.Y : bounds.Max.Y,
                (corner & 4) == 0 ? bounds.Min.Z : bounds.Max.Z);

            result = result.Include(transform.TransformPoint(point));
        }

        return result;
    }
}

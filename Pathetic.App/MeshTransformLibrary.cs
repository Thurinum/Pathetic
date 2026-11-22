using System.Numerics;
using Pathetic.Renderer.Buffers;

namespace Pathetic.App;

public static class MeshTransformExtensions
{
    public static SlangGen.Primitives.MeshTransform FromMatrix(Matrix4x4 matrix)
    {
        if (!Matrix4x4.Invert(matrix, out Matrix4x4 inverse))
            inverse = Matrix4x4.Identity;

        Matrix4x4 normal = Matrix4x4.Transpose(inverse);

        return new SlangGen.Primitives.MeshTransform
        {
            InverseX = new Vector3(inverse.M11, inverse.M12, inverse.M13),
            InverseY = new Vector3(inverse.M21, inverse.M22, inverse.M23),
            InverseZ = new Vector3(inverse.M31, inverse.M32, inverse.M33),
            InverseOffset = new Vector3(inverse.M41, inverse.M42, inverse.M43),
            NormalX = new Vector3(normal.M11, normal.M12, normal.M13),
            NormalY = new Vector3(normal.M21, normal.M22, normal.M23),
            NormalZ = new Vector3(normal.M31, normal.M32, normal.M33),
            ForwardX = new Vector3(matrix.M11, matrix.M12, matrix.M13),
            ForwardY = new Vector3(matrix.M21, matrix.M22, matrix.M23),
            ForwardZ = new Vector3(matrix.M31, matrix.M32, matrix.M33),
            ForwardOffset = new Vector3(matrix.M41, matrix.M42, matrix.M43)
        };
    }

    public static Vector3 TransformPoint(this SlangGen.Primitives.MeshTransform transform, Vector3 point) =>
        point.X * transform.ForwardX + point.Y * transform.ForwardY + point.Z * transform.ForwardZ + transform.ForwardOffset;
}

public sealed class MeshTransformLibrary(StructuredBuffer<SlangGen.Primitives.MeshTransform> transforms)
{
    public const uint None = uint.MaxValue;

    private readonly Stack<uint> _free = [];

    public uint Add(in SlangGen.Primitives.MeshTransform transform)
    {
        if (_free.Count > 0)
        {
            uint index = _free.Pop();
            transforms.UpdateAt((int)index, transform);
            return index;
        }

        return (uint)transforms.Add(transform);
    }

    public void Update(uint index, in SlangGen.Primitives.MeshTransform transform) => transforms.UpdateAt((int)index, transform);

    public void Remove(uint index) => _free.Push(index);
}

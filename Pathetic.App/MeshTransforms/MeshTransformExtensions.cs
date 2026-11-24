using System.Numerics;

namespace Pathetic.App.MeshTransforms;

public static class MeshTransformExtensions
{
    public static Gpu.MeshTransform FromMatrix(Matrix4x4 matrix)
    {
        if (!Matrix4x4.Invert(matrix, out Matrix4x4 inverse))
            inverse = Matrix4x4.Identity;

        Matrix4x4 normal = Matrix4x4.Transpose(inverse);

        return new Gpu.MeshTransform
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

    public static Vector3 TransformPoint(this Gpu.MeshTransform transform, Vector3 point) =>
        point.X * transform.ForwardX + point.Y * transform.ForwardY + point.Z * transform.ForwardZ + transform.ForwardOffset;
}

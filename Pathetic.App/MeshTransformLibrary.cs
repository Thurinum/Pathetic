using System.Numerics;
using System.Runtime.InteropServices;
using Pathetic.Renderer.Buffers;

namespace Pathetic.App;

[StructLayout(LayoutKind.Explicit, Size = 132)]
public record struct MeshTransform
{
    public const uint None = uint.MaxValue;

    [FieldOffset(00)] public Vector3 InverseX;
    [FieldOffset(12)] public Vector3 InverseY;
    [FieldOffset(24)] public Vector3 InverseZ;
    [FieldOffset(36)] public Vector3 InverseOffset;
    [FieldOffset(48)] public Vector3 NormalX;
    [FieldOffset(60)] public Vector3 NormalY;
    [FieldOffset(72)] public Vector3 NormalZ;
    [FieldOffset(84)] public Vector3 ForwardX;
    [FieldOffset(96)] public Vector3 ForwardY;
    [FieldOffset(108)] public Vector3 ForwardZ;
    [FieldOffset(120)] public Vector3 ForwardOffset;

    public static MeshTransform FromMatrix(Matrix4x4 matrix)
    {
        if (!Matrix4x4.Invert(matrix, out Matrix4x4 inverse))
            inverse = Matrix4x4.Identity;

        Matrix4x4 normal = Matrix4x4.Transpose(inverse);

        return new MeshTransform
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

    public readonly Vector3 TransformPoint(Vector3 point) =>
        point.X * ForwardX + point.Y * ForwardY + point.Z * ForwardZ + ForwardOffset;
}

public sealed class MeshTransformLibrary(StructuredBuffer<MeshTransform> transforms)
{
    private readonly Stack<uint> _free = [];

    public uint Add(in MeshTransform transform)
    {
        if (_free.Count > 0)
        {
            uint index = _free.Pop();
            transforms.UpdateAt((int)index, transform);
            return index;
        }

        return (uint)transforms.Add(transform);
    }

    public void Update(uint index, in MeshTransform transform) => transforms.UpdateAt((int)index, transform);

    public void Remove(uint index) => _free.Push(index);
}

using System.Numerics;

namespace Pathetic.Renderer.Utils;

// ReSharper disable InconsistentNaming
public static class Vector2Extensions
{
    extension(Vector2 v)
    {
        public Vector2 XX => new(v.X, v.X);
        public Vector2 XY => new(v.X, v.Y);
        public Vector2 YX => new(v.Y, v.X);
        public Vector2 YY => new(v.Y, v.Y);
        public Vector3 XXX => new(v.X, v.X, v.X);
        public Vector3 XXY => new(v.X, v.X, v.Y);
        public Vector3 XYX => new(v.X, v.Y, v.X);
        public Vector3 XYY => new(v.X, v.Y, v.Y);
        public Vector3 YXX => new(v.Y, v.X, v.X);
        public Vector3 YXY => new(v.Y, v.X, v.Y);
        public Vector3 YYX => new(v.Y, v.Y, v.X);
        public Vector3 YYY => new(v.Y, v.Y, v.Y);
        public Vector4 XXXX => new(v.X, v.X, v.X, v.X);
        public Vector4 XXXY => new(v.X, v.X, v.X, v.Y);
        public Vector4 XXYX => new(v.X, v.X, v.Y, v.X);
        public Vector4 XXYY => new(v.X, v.X, v.Y, v.Y);
        public Vector4 XYXX => new(v.X, v.Y, v.X, v.X);
        public Vector4 XYXY => new(v.X, v.Y, v.X, v.Y);
        public Vector4 XYYX => new(v.X, v.Y, v.Y, v.X);
        public Vector4 XYYY => new(v.X, v.Y, v.Y, v.Y);
        public Vector4 YXXX => new(v.Y, v.X, v.X, v.X);
        public Vector4 YXXY => new(v.Y, v.X, v.X, v.Y);
        public Vector4 YXYX => new(v.Y, v.X, v.Y, v.X);
        public Vector4 YXYY => new(v.Y, v.X, v.Y, v.Y);
        public Vector4 YYXX => new(v.Y, v.Y, v.X, v.X);
        public Vector4 YYXY => new(v.Y, v.Y, v.X, v.Y);
        public Vector4 YYYX => new(v.Y, v.Y, v.Y, v.X);
        public Vector4 YYYY => new(v.Y, v.Y, v.Y, v.Y);
    }

}

public static class Vector3Extensions
{
    extension(Vector3 v)
    {
        public Vector2 XX => new(v.X, v.X);
        public Vector2 XY => new(v.X, v.Y);
        public Vector2 XZ => new(v.X, v.Z);
        public Vector2 YX => new(v.Y, v.X);
        public Vector2 YY => new(v.Y, v.Y);
        public Vector2 YZ => new(v.Y, v.Z);
        public Vector2 ZX => new(v.Z, v.X);
        public Vector2 ZY => new(v.Z, v.Y);
        public Vector2 ZZ => new(v.Z, v.Z);
        public Vector3 XXX => new(v.X, v.X, v.X);
        public Vector3 XXY => new(v.X, v.X, v.Y);
        public Vector3 XXZ => new(v.X, v.X, v.Z);
        public Vector3 XYX => new(v.X, v.Y, v.X);
        public Vector3 XYY => new(v.X, v.Y, v.Y);
        public Vector3 XYZ => new(v.X, v.Y, v.Z);
        public Vector3 XZX => new(v.X, v.Z, v.X);
        public Vector3 XZY => new(v.X, v.Z, v.Y);
        public Vector3 XZZ => new(v.X, v.Z, v.Z);
        public Vector3 YXX => new(v.Y, v.X, v.X);
        public Vector3 YXY => new(v.Y, v.X, v.Y);
        public Vector3 YXZ => new(v.Y, v.X, v.Z);
        public Vector3 YYX => new(v.Y, v.Y, v.X);
        public Vector3 YYY => new(v.Y, v.Y, v.Y);
        public Vector3 YYZ => new(v.Y, v.Y, v.Z);
        public Vector3 YZX => new(v.Y, v.Z, v.X);
        public Vector3 YZY => new(v.Y, v.Z, v.Y);
        public Vector3 YZZ => new(v.Y, v.Z, v.Z);
        public Vector3 ZXX => new(v.Z, v.X, v.X);
        public Vector3 ZXY => new(v.Z, v.X, v.Y);
        public Vector3 ZXZ => new(v.Z, v.X, v.Z);
        public Vector3 ZYX => new(v.Z, v.Y, v.X);
        public Vector3 ZYY => new(v.Z, v.Y, v.Y);
        public Vector3 ZYZ => new(v.Z, v.Y, v.Z);
        public Vector3 ZZX => new(v.Z, v.Z, v.X);
        public Vector3 ZZY => new(v.Z, v.Z, v.Y);
        public Vector3 ZZZ => new(v.Z, v.Z, v.Z);
        public Vector4 XXXX => new(v.X, v.X, v.X, v.X);
        public Vector4 XXXY => new(v.X, v.X, v.X, v.Y);
        public Vector4 XXXZ => new(v.X, v.X, v.X, v.Z);
        public Vector4 XXYX => new(v.X, v.X, v.Y, v.X);
        public Vector4 XXYY => new(v.X, v.X, v.Y, v.Y);
        public Vector4 XXYZ => new(v.X, v.X, v.Y, v.Z);
        public Vector4 XXZX => new(v.X, v.X, v.Z, v.X);
        public Vector4 XXZY => new(v.X, v.X, v.Z, v.Y);
        public Vector4 XXZZ => new(v.X, v.X, v.Z, v.Z);
        public Vector4 XYXX => new(v.X, v.Y, v.X, v.X);
        public Vector4 XYXY => new(v.X, v.Y, v.X, v.Y);
        public Vector4 XYXZ => new(v.X, v.Y, v.X, v.Z);
        public Vector4 XYYX => new(v.X, v.Y, v.Y, v.X);
        public Vector4 XYYY => new(v.X, v.Y, v.Y, v.Y);
        public Vector4 XYYZ => new(v.X, v.Y, v.Y, v.Z);
        public Vector4 XYZX => new(v.X, v.Y, v.Z, v.X);
        public Vector4 XYZY => new(v.X, v.Y, v.Z, v.Y);
        public Vector4 XYZZ => new(v.X, v.Y, v.Z, v.Z);
        public Vector4 XZXX => new(v.X, v.Z, v.X, v.X);
        public Vector4 XZXY => new(v.X, v.Z, v.X, v.Y);
        public Vector4 XZXZ => new(v.X, v.Z, v.X, v.Z);
        public Vector4 XZYX => new(v.X, v.Z, v.Y, v.X);
        public Vector4 XZYY => new(v.X, v.Z, v.Y, v.Y);
        public Vector4 XZYZ => new(v.X, v.Z, v.Y, v.Z);
        public Vector4 XZZX => new(v.X, v.Z, v.Z, v.X);
        public Vector4 XZZY => new(v.X, v.Z, v.Z, v.Y);
        public Vector4 XZZZ => new(v.X, v.Z, v.Z, v.Z);
        public Vector4 YXXX => new(v.Y, v.X, v.X, v.X);
        public Vector4 YXXY => new(v.Y, v.X, v.X, v.Y);
        public Vector4 YXXZ => new(v.Y, v.X, v.X, v.Z);
        public Vector4 YXYX => new(v.Y, v.X, v.Y, v.X);
        public Vector4 YXYY => new(v.Y, v.X, v.Y, v.Y);
        public Vector4 YXYZ => new(v.Y, v.X, v.Y, v.Z);
        public Vector4 YXZX => new(v.Y, v.X, v.Z, v.X);
        public Vector4 YXZY => new(v.Y, v.X, v.Z, v.Y);
        public Vector4 YXZZ => new(v.Y, v.X, v.Z, v.Z);
        public Vector4 YYXX => new(v.Y, v.Y, v.X, v.X);
        public Vector4 YYXY => new(v.Y, v.Y, v.X, v.Y);
        public Vector4 YYXZ => new(v.Y, v.Y, v.X, v.Z);
        public Vector4 YYYX => new(v.Y, v.Y, v.Y, v.X);
        public Vector4 YYYY => new(v.Y, v.Y, v.Y, v.Y);
        public Vector4 YYYZ => new(v.Y, v.Y, v.Y, v.Z);
        public Vector4 YYZX => new(v.Y, v.Y, v.Z, v.X);
        public Vector4 YYZY => new(v.Y, v.Y, v.Z, v.Y);
        public Vector4 YYZZ => new(v.Y, v.Y, v.Z, v.Z);
        public Vector4 YZXX => new(v.Y, v.Z, v.X, v.X);
        public Vector4 YZXY => new(v.Y, v.Z, v.X, v.Y);
        public Vector4 YZXZ => new(v.Y, v.Z, v.X, v.Z);
        public Vector4 YZYX => new(v.Y, v.Z, v.Y, v.X);
        public Vector4 YZYY => new(v.Y, v.Z, v.Y, v.Y);
        public Vector4 YZYZ => new(v.Y, v.Z, v.Y, v.Z);
        public Vector4 YZZX => new(v.Y, v.Z, v.Z, v.X);
        public Vector4 YZZY => new(v.Y, v.Z, v.Z, v.Y);
        public Vector4 YZZZ => new(v.Y, v.Z, v.Z, v.Z);
        public Vector4 ZXXX => new(v.Z, v.X, v.X, v.X);
        public Vector4 ZXXY => new(v.Z, v.X, v.X, v.Y);
        public Vector4 ZXXZ => new(v.Z, v.X, v.X, v.Z);
        public Vector4 ZXYX => new(v.Z, v.X, v.Y, v.X);
        public Vector4 ZXYY => new(v.Z, v.X, v.Y, v.Y);
        public Vector4 ZXYZ => new(v.Z, v.X, v.Y, v.Z);
        public Vector4 ZXZX => new(v.Z, v.X, v.Z, v.X);
        public Vector4 ZXZY => new(v.Z, v.X, v.Z, v.Y);
        public Vector4 ZXZZ => new(v.Z, v.X, v.Z, v.Z);
        public Vector4 ZYXX => new(v.Z, v.Y, v.X, v.X);
        public Vector4 ZYXY => new(v.Z, v.Y, v.X, v.Y);
        public Vector4 ZYXZ => new(v.Z, v.Y, v.X, v.Z);
        public Vector4 ZYYX => new(v.Z, v.Y, v.Y, v.X);
        public Vector4 ZYYY => new(v.Z, v.Y, v.Y, v.Y);
        public Vector4 ZYYZ => new(v.Z, v.Y, v.Y, v.Z);
        public Vector4 ZYZX => new(v.Z, v.Y, v.Z, v.X);
        public Vector4 ZYZY => new(v.Z, v.Y, v.Z, v.Y);
        public Vector4 ZYZZ => new(v.Z, v.Y, v.Z, v.Z);
        public Vector4 ZZXX => new(v.Z, v.Z, v.X, v.X);
        public Vector4 ZZXY => new(v.Z, v.Z, v.X, v.Y);
        public Vector4 ZZXZ => new(v.Z, v.Z, v.X, v.Z);
        public Vector4 ZZYX => new(v.Z, v.Z, v.Y, v.X);
        public Vector4 ZZYY => new(v.Z, v.Z, v.Y, v.Y);
        public Vector4 ZZYZ => new(v.Z, v.Z, v.Y, v.Z);
        public Vector4 ZZZX => new(v.Z, v.Z, v.Z, v.X);
        public Vector4 ZZZY => new(v.Z, v.Z, v.Z, v.Y);
        public Vector4 ZZZZ => new(v.Z, v.Z, v.Z, v.Z);
    }

}

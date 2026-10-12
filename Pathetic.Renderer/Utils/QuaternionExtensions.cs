using System.Numerics;

namespace Pathetic.Renderer.Utils;

public static class QuaternionExtensions
{
    extension(Quaternion)
    {
        public static Quaternion FromEuler(Vector3 degrees)
        {
            return Quaternion.CreateFromYawPitchRoll(
                float.DegreesToRadians(degrees.Y),
                float.DegreesToRadians(degrees.X),
                float.DegreesToRadians(degrees.Z));
        }
    }
}
using System.Numerics;

namespace Pathetic.App.RenderPasses;

public struct Ray(Vector3 origin, Vector3 direction)
{
    public Vector3 Origin { get; set; } = origin;
    public Vector3 Direction { get; set; } = direction;
}

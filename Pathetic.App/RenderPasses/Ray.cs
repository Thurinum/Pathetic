using System.Numerics;

namespace Pathetic.App.RenderPasses;

public sealed class Ray
{
    public Vector3 Origin { get; set; } = Vector3.Zero;
    public Vector3 Direction { get; set; } = -Vector3.UnitZ;
}

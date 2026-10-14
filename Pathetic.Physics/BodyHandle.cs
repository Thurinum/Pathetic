using System.Numerics;

namespace Pathetic.Physics;

// Opaque, backend-agnostic reference to a simulated body. It never exposes the middleware's own handles.
public readonly record struct BodyHandle(int Index, int Generation)
{
    public static BodyHandle None => new(-1, 0);

    public bool IsValid => Index >= 0;
}

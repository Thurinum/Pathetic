using System.Numerics;

namespace Pathetic.Physics;

// Backend-neutral collision geometry. Adapters translate these into whatever shape types the
// middleware exposes; the rest of the engine never sees the middleware.
public abstract record Shape;

public sealed record SphereShape(float Radius) : Shape;

// Size is the full extent along each axis (not the half extent).
public sealed record BoxShape(Vector3 Size) : Shape;

// Length is the distance between the hemispherical caps; the symmetry axis is Y.
public sealed record CapsuleShape(float Radius, float Length) : Shape;

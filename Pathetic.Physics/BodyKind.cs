namespace Pathetic.Physics;

// How the solver treats a body. Static and kinematic bodies are driven by user code, dynamic ones by the solver.
public enum BodyKind
{
    Static,
    Kinematic,
    Dynamic
}

using System.Numerics;

namespace Pathetic.Input;

// ReSharper disable InconsistentNaming
public sealed class InputAction_Bool : InputAction<bool>
{
    protected override bool MeetsThreshold(bool value) => value;
    protected override bool Combine(bool total, bool value) => total | value;
}

public sealed class InputAction_Float : InputAction<float>
{
    public float Threshold { get; set; } = 0.5f;

    protected override bool MeetsThreshold(float value) => MathF.Abs(value) > Threshold;
    protected override float Combine(float total, float value) => total + value;
}

public sealed class InputAction_Vector2 : InputAction<Vector2>
{
    public float Threshold { get; set; } = 0.5f;

    protected override bool MeetsThreshold(Vector2 value) => value.LengthSquared() > Threshold * Threshold;
    protected override Vector2 Combine(Vector2 total, Vector2 value) => total + value;
}

public sealed class InputAction_Vector3 : InputAction<Vector3>
{
    public float Threshold { get; set; } = 0.5f;

    protected override bool MeetsThreshold(Vector3 value) => value.LengthSquared() > Threshold * Threshold;
    protected override Vector3 Combine(Vector3 total, Vector3 value) => total + value;
}
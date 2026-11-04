using System.Numerics;

namespace Pathetic.Input;

public readonly record struct InputValueHandle(string DeviceKey, string ControlKey);

public interface IInputValue
{
    bool IsActive { get; }
    bool WasActive { get; }
    bool IsTriggered => IsActive && !WasActive;

    void Commit();
}

public sealed class InputValue<T> : IInputValue where T : struct
{
    static InputValue()
    {
        if (!IsSupported(typeof(T)))
        {
            throw new NotSupportedException($"{typeof(T)} input values unsupported. Must be one of bool, float, Vector2, Vector3.");
        }
    }

    public Type Type { get; } = typeof(T);
    public bool IsActive => Active(Value);
    public bool WasActive => Active(PreviousValue);
    
    public float Threshold { get; set; } = 0.5f;
    public T Value { get; set; }
    public T PreviousValue { get; set; }

    public void Commit() => PreviousValue = Value;

    private static bool IsSupported(Type type)
    {
        return type == typeof(bool) ||
               type == typeof(float) ||
               type == typeof(Vector2) ||
               type == typeof(Vector3);
    }

    private bool Active(T value)
    {
        return value switch
        {
            bool b => b,
            float f => Math.Abs(f) > Threshold,
            Vector2 v => v.LengthSquared() > Threshold * Threshold,
            Vector3 v => v.LengthSquared() > Threshold * Threshold,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}
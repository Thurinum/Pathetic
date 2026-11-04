using System.Numerics;

namespace Pathetic.Input;

public readonly record struct InputValueHandle(string DeviceKey, string ControlKey);

public interface IInputValue
{
    Type Type { get; }
    void Commit();
}

public interface IInputValue<T> : IInputValue where T : struct
{
    T Value { get; }
    T PreviousValue { get; }
}

public sealed class InputValue<T> : IInputValue<T> where T : struct
{
    static InputValue()
    {
        if (!IsSupported(typeof(T)))
        {
            throw new NotSupportedException($"{typeof(T)} input values unsupported. Must be one of bool, float, Vector2, Vector3.");
        }
    }

    public Type Type { get; } = typeof(T);
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
}

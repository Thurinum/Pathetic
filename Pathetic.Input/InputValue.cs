using System.Numerics;

namespace Pathetic.Input;

public readonly record struct InputValueHandle(string DeviceKey, string ControlKey)
{
    public IInputValue Resolve(IReadOnlyDictionary<string, InputDevice> devices)
    {
        if (!devices.TryGetValue(DeviceKey, out InputDevice? device))
        {
            throw new InvalidOperationException($"Unknown device '{DeviceKey}'. Known devices: {string.Join(", ", devices.Keys)}.");
        }

        if (!device.Snapshot.TryGetValue(ControlKey, out IInputValue? value))
        {
            throw new InvalidOperationException($"Unknown control '{ControlKey}' on device '{DeviceKey}'.");
        }

        return value;
    }

    public IInputValue<T> Resolve<T>(IReadOnlyDictionary<string, InputDevice> devices) where T : struct
    {
        return Resolve(devices) as IInputValue<T> ?? throw new InvalidOperationException($"Control '{ControlKey}' on device '{DeviceKey}' is not of type '{typeof(T).Name}'.");
    }
}

public interface IInputValue
{
    Type Type { get; }
    object Value { get; }
    void Commit();
}

public interface IInputValue<out T> : IInputValue where T : struct
{
    object IInputValue.Value => Value;
    new T Value { get; }
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

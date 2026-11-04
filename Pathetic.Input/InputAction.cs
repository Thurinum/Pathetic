using System.Numerics;

namespace Pathetic.Input;

public abstract class InputAction
{
    public required string Name { get; init; }
    public required List<InputValueHandle> Controls { get; init; }
    
    public event Action? Triggered;
    protected void OnTriggered() => Triggered?.Invoke();

    internal abstract void Bind(IReadOnlyDictionary<string, InputDevice> devices);
    internal abstract void Evaluate();
}

public abstract class InputAction<T> : InputAction where T : struct
{
    private IInputValue<T>[] _controls = [];

    public T Value { get; private set; }

    protected abstract bool IsActive(T value);
    protected abstract T Combine(T total, T value);

    internal override void Bind(IReadOnlyDictionary<string, InputDevice> devices)
    {
        var controls = new IInputValue<T>[Controls.Count];

        for (int i = 0; i < Controls.Count; i++)
        {
            InputValueHandle handle = Controls[i];

            if (!devices.TryGetValue(handle.DeviceKey, out InputDevice? device))
            {
                throw new InvalidOperationException($"Unknown device '{handle.DeviceKey}'. Known devices: {string.Join(", ", devices.Keys)}.");
            }

            if (!device.Controls.TryGetValue(handle.ControlKey, out IInputValue? value))
            {
                throw new InvalidOperationException($"Unknown control '{handle.ControlKey}' on device '{handle.DeviceKey}'.");
            }

            controls[i] = value as IInputValue<T> ?? throw new InvalidOperationException($"Control '{handle.ControlKey}' on device '{handle.DeviceKey}' is {value.Type}, but '{Name}' is {typeof(T)}.");
        }

        _controls = controls;
    }

    internal override void Evaluate()
    {
        bool isActive = false;
        bool wasActive = false;
        T value = default;

        foreach (IInputValue<T> control in _controls)
        {
            value = Combine(value, control.Value);
            isActive |= IsActive(control.Value);
            wasActive |= IsActive(control.PreviousValue);
        }

        Value = value;

        if (isActive && !wasActive)
        {
            OnTriggered();
        }
    }
}

// ReSharper disable InconsistentNaming
public sealed class InputAction_Bool : InputAction<bool>
{
    protected override bool IsActive(bool value) => value;
    protected override bool Combine(bool total, bool value) => total | value;
}

public sealed class InputAction_Float : InputAction<float>
{
    public float Threshold { get; set; } = 0.5f;

    protected override bool IsActive(float value) => MathF.Abs(value) > Threshold;
    protected override float Combine(float total, float value) => total + value;
}

public sealed class InputAction_Vector2 : InputAction<Vector2>
{
    public float Threshold { get; set; } = 0.5f;

    protected override bool IsActive(Vector2 value) => value.LengthSquared() > Threshold * Threshold;
    protected override Vector2 Combine(Vector2 total, Vector2 value) => total + value;
}

public sealed class InputAction_Vector3 : InputAction<Vector3>
{
    public float Threshold { get; set; } = 0.5f;

    protected override bool IsActive(Vector3 value) => value.LengthSquared() > Threshold * Threshold;
    protected override Vector3 Combine(Vector3 total, Vector3 value) => total + value;
}

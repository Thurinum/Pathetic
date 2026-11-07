using System.Numerics;

namespace Pathetic.Input;

public abstract class InputAction
{
    public required string Name { get; init; }
    public required List<InputBindingSpec> Bindings { get; init; }
    
    public event Action? Triggered;
    protected void OnTriggered() => Triggered?.Invoke();

    internal abstract void Bind(IReadOnlyDictionary<string, InputDevice> devices);
    internal abstract void Evaluate(float deltaTime, bool enabled);
}

public abstract class InputAction<T> : InputAction where T : struct
{
    private InputBinding<T>[] _bindings = [];

    public T Value { get; private set; }

    protected abstract bool IsActive(T value);
    protected abstract T Combine(T total, T value);

    internal override void Bind(IReadOnlyDictionary<string, InputDevice> devices)
    {
        var bindings = new InputBinding<T>[Bindings.Count];

        for (int i = 0; i < Bindings.Count; i++)
        {
            InputBindingSpec spec = Bindings[i];

            var source = spec.Source.Resolve(devices);
            ValidateProcessors(source, spec, i);

            bindings[i] = new InputBinding<T>
            {
                Source = source,
                Chords = [.. spec.Chords.Select(m => m.Resolve<bool>(devices))],
                Processors = [.. spec.Processors]
            };
        }

        _bindings = bindings;
    }

    private void ValidateProcessors(IInputValue source, InputBindingSpec spec, int i)
    {
        var type = source.Type;
            
        foreach (var processor in spec.Processors)
        {
            if (processor.InputType != type)
            {
                throw new InvalidOperationException($"Action '{Name}' ({typeof(T).Name}) binding {i}: processor '{processor.Name}' expects {processor.InputType.Name}, but the previous step is {type.Name}.");
            }

            type = processor.OutputType;
        }

        if (type != typeof(T))
        {
            throw new InvalidOperationException($"Action '{Name}' ({typeof(T).Name}) binding {i} produces {type.Name}.");
        }
    }

    internal override void Evaluate(float deltaTime, bool enabled)
    {
        if (!enabled)
        {
            Value = default;
            return;
        }
        
        T previousValue = Value;
        T value = default;

        foreach (InputBinding<T> binding in _bindings)
        {
            if (binding.Chords.Any(c => !c.Value))
                continue;

            object processedValue = binding.Source.Value;
            foreach (IInputProcessor processor in binding.Processors)
            {
                processedValue = processor.Process(processedValue, deltaTime);
            }

            value = Combine(value, (T)processedValue);
        }

        Value = value;

        if (IsActive(Value) && !IsActive(previousValue))
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

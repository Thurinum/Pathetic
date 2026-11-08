using System.Numerics;

namespace Pathetic.Input;

public abstract class InputAction
{
    public required string Name { get; init; }
    public required List<InputBindingSpec> Bindings { get; init; }
    public required IInputProcessor[] Processors { get; init; }

    public event Action? Initiated;
    public event Action? Triggered;
    public event Action? Completed;
    public event Action? Cancelled;
    protected void OnInitiated() => Initiated?.Invoke();
    protected void OnTriggered() => Triggered?.Invoke();
    protected void OnCompleted() => Completed?.Invoke();
    protected void OnCancelled() => Cancelled?.Invoke();

    internal abstract void Bind(IReadOnlyDictionary<string, InputDevice> devices);
    internal abstract void Evaluate(float deltaTime, bool enabled);
}

public abstract class InputAction<T> : InputAction where T : struct
{
    private InputBinding<T>[] _bindings = [];

    public T Value { get; private set; }
    public required IInputTrigger<T>[] Triggers { get; init; }

    protected abstract bool MeetsThreshold(T value);
    protected abstract T Combine(T total, T value);

    internal override void Bind(IReadOnlyDictionary<string, InputDevice> devices)
    {
        ValidateProcessorChain(Processors, typeof(T), bindingIndex: null);

        var bindings = new InputBinding<T>[Bindings.Count];

        for (int i = 0; i < Bindings.Count; i++)
        {
            InputBindingSpec spec = Bindings[i];

            var source = spec.Source.Resolve(devices);
            ValidateProcessorChain(spec.Processors, source.Type, i);

            bindings[i] = new InputBinding<T>
            {
                Source = source,
                Chords = [.. spec.Chords.Select(m => m.Resolve<bool>(devices))],
                Processors = [.. spec.Processors]
            };
        }

        _bindings = bindings;
    }

    private void ValidateProcessorChain(IReadOnlyList<IInputProcessor> processors, Type type, int? bindingIndex)
    {
        string context = bindingIndex.HasValue
            ? $"Action '{Name}' ({typeof(T).Name}) binding {bindingIndex.Value}"
            : $"Action '{Name}' ({typeof(T).Name})";

        foreach (IInputProcessor processor in processors)
        {
            if (processor.InputType != type)
            {
                throw new InvalidOperationException($"{context}: processor '{processor.Name}' expects {processor.InputType.Name}, but the previous step is {type.Name}.");
            }

            type = processor.OutputType;
        }

        if (type != typeof(T))
        {
            throw new InvalidOperationException($"{context} produces {type.Name}.");
        }
    }

    internal override void Evaluate(float deltaTime, bool enabled)
    {
        T previousValue = Value;
        
        if (!enabled)
        {
            Value = default;
            
            if (MeetsThreshold(previousValue))
            {
                OnCancelled();
                ResetTriggers();
            }
            
            return;
        }
        
        T value = CombineBindings(deltaTime);

        foreach (var processor in Processors)
        {
            value = (T)processor.Process(value, deltaTime);
        }

        Value = value;
        
        bool wasInitiated = MeetsThreshold(previousValue);
        bool isInitiated = MeetsThreshold(Value);
        if (!wasInitiated && isInitiated)
        {
            OnInitiated();
        }
        else if (wasInitiated && !isInitiated)
        {
            TryTrigger(deltaTime, isCompleted: true);
            OnCompleted();
            ResetTriggers();
        }
        
        if (isInitiated)
        {
            TryTrigger(deltaTime, isCompleted: false);
        }
    }

    private T CombineBindings(float deltaTime)
    {
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

        return value;
    }

    private void TryTrigger(float deltaTime, bool isCompleted)
    {
        bool canTrigger = true;
        
        foreach (IInputTrigger<T> trigger in Triggers)
        {
            canTrigger &= isCompleted 
                ? trigger.CanComplete(Value, deltaTime) 
                : trigger.CanTrigger(Value, deltaTime);
        }

        if (canTrigger)
        {
            OnTriggered();
        }
    }

    private void ResetTriggers()
    {
        foreach (var trigger in Triggers)
        {
            trigger.Reset();
        }
    }
}

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

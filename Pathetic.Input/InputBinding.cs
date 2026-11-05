namespace Pathetic.Input;

public sealed record InputBindingSpec(
    InputValueHandle Source,
    IReadOnlyList<InputValueHandle> Modifiers,
    IReadOnlyList<IInputProcessor> Processors);
    
public sealed class InputBinding<T> where T : struct
{
    public required IInputValue Source { get; init; }
    public required IInputValue<bool>[] Modifiers { get; init; }
    public required IInputProcessor[] Processors { get; init; }
    public T Previous { get; set; }
}
namespace Pathetic.Input;

public sealed record InputBindingSpec(
    InputValueHandle Source,
    IReadOnlyList<InputValueHandle> Chords,
    IReadOnlyList<IInputProcessor> Processors);
    
public sealed class InputBinding<T> where T : struct
{
    public required IInputValue Source { get; init; }
    public required IInputValue<bool>[] Chords { get; init; }
    public required IInputProcessor[] Processors { get; init; }
    public T Previous { get; set; }
}
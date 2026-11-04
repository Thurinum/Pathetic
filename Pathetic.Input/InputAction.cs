namespace Pathetic.Input;

public class InputAction
{
    public required string Name { get; init; }
    public required List<InputValueHandle> Controls { get; init; }
    
    public event Action? Triggered;
    public void OnTriggered()
    {
        Triggered?.Invoke();
    }
}
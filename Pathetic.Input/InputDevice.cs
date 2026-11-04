namespace Pathetic.Input;

public abstract class InputDevice
{
    public abstract string Name { get; }
    public abstract Dictionary<string, IInputValue> Controls { get; }
    public abstract void Update();
}
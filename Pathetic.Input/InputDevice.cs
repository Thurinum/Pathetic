namespace Pathetic.Input;

public abstract class InputDevice
{
    public abstract string Name { get; }
    public abstract Dictionary<string, IInputValue> Snapshot { get; }

    public abstract void Update(float deltaTime);
}
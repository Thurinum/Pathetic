namespace Pathetic.Input;

public abstract class InputDevice
{
    public abstract string Name { get; }
    public abstract Dictionary<string, IInputValue> Snapshot { get; }

    public void Update(float deltaTime)
    {
        foreach (IInputValue control in Snapshot.Values)
        {
            control.Commit();
        }

        Poll(deltaTime);
    }

    protected abstract void Poll(float deltaTime);
}
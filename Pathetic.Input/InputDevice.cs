namespace Pathetic.Input;

public abstract class InputDevice
{
    public abstract string Name { get; }
    public abstract Dictionary<string, IInputValue> Controls { get; }

    public void Update()
    {
        foreach (IInputValue control in Controls.Values)
        {
            control.Commit();
        }

        Poll();
    }

    protected abstract void Poll();
}
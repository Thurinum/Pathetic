namespace Pathetic.Input;

public class InputSystem
{
    private readonly List<InputDevice> _devices;
    private readonly List<InputContext> _contexts;

    public InputSystem(IEnumerable<InputDevice> devices, IEnumerable<InputContext> contexts)
    {
        _devices = devices.ToList();
        _contexts = contexts.ToList();
        var devicesByName = _devices.ToDictionary(d => d.Name);

        foreach (InputContext context in _contexts)
        {
            foreach (InputAction action in context.Actions)
            {
                try
                {
                    action.Bind(devicesByName);
                }
                catch (InvalidOperationException e)
                {
                    throw new InvalidOperationException($"Failed to bind '{context.Name}.{action.Name}'.", e);
                }
            }
        }
    }

    public void Update()
    {
        UpdateDevices();
        UpdateActions();
    }

    private void UpdateDevices()
    {
        foreach (InputDevice device in _devices)
        {
            device.Update();
        }
    }

    private void UpdateActions()
    {
        foreach (InputContext ctx in _contexts)
        {
            if (!ctx.Enabled)
                continue;
            
            foreach (InputAction action in ctx.Actions)
            {
                action.Evaluate();
            }
        }
    }
}

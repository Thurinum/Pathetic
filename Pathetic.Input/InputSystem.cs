namespace Pathetic.Input;

public sealed class InputSystem
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

    public void Update(float deltaTime)
    {
        UpdateDevices(deltaTime);
        UpdateActions(deltaTime);
    }

    private void UpdateDevices(float deltaTime)
    {
        foreach (InputDevice device in _devices)
        {
            device.Update(deltaTime);
        }
    }

    private void UpdateActions(float deltaTime)
    {
        foreach (InputContext ctx in _contexts)
        {
            foreach (InputAction action in ctx.Actions)
            {
                action.Evaluate(deltaTime, ctx.Enabled);
            }
        }
    }
}

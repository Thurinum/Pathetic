namespace Pathetic.Input;

public class InputSystem(IEnumerable<InputDevice> devices, IEnumerable<InputContext> contexts)
{
    private readonly List<InputDevice> _devices = devices.ToList();
    private readonly List<InputContext> _contexts = contexts.ToList();
    private readonly Dictionary<string, InputDevice> _devicesByName = devices.ToDictionary(d => d.Name);

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
                UpdateAction(action);
            }
        }
    }

    private void UpdateAction(InputAction action)
    {
        bool isTriggered = false;
        
        foreach (var control in action.Controls.Select(ResolveControl))
        {
            isTriggered |= control.IsTriggered;
        }

        if (isTriggered)
        {
            action.OnTriggered();
        }
    }

    private IInputValue ResolveControl(InputValueHandle hndl)
    {
        return _devicesByName[hndl.DeviceKey].Controls[hndl.ControlKey];
    }
}

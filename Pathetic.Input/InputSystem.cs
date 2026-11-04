namespace Pathetic.Input;

public record InputActionHandle(string ContextName, string ActionName);

public class InputSystem(List<InputDevice> devices, List<InputContext> contexts)
{
    private readonly Dictionary<string, InputDevice> _devices = devices.ToDictionary(d => d.Name);

    public void Update(float deltaTime)
    {
        UpdateDevices();
        UpdateActions();
    }

    private void UpdateDevices()
    {
        foreach (InputDevice device in devices)
        {
            device.Update();
        }
    }

    private void UpdateActions()
    {
        foreach (InputContext ctx in contexts)
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
        return _devices[hndl.DeviceKey].Controls[hndl.ControlKey];
    }
}

using Pathetic.Input;

namespace Pathetic.App;

public static class GameInput
{
    public static readonly InputAction CameraOrbit =
        new("CameraOrbit", InputActionType.Button, "Mouse/Right");

    public static readonly InputAction CameraBoost =
        new("CameraBoost", InputActionType.Button, "Keyboard/LeftShift");

    public static readonly InputContext Camera =
        new("Camera", CameraOrbit, CameraBoost);
}

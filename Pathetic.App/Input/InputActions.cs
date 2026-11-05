using Pathetic.Input;

namespace Pathetic.App.Input;

public sealed class InputActions
{
    public EditorViewportContext EditorViewport { get; } = new();
    public InputContext[] All { get; }
    
    public InputActions()
    {
        All = [EditorViewport.Context];
    }
    
    public sealed class EditorViewportContext
    {
        public InputAction_Bool Focus { get; } = new()
        {
            Name = "Focus",
            Bindings =
            [
                new InputBindingSpec(Handle("Mouse and Keyboard", "F"), [], []),
            ],
        };

        public InputAction_Vector2 Orbit { get; } = new()
        {
            Name = "Orbit",
            Bindings =
            [
                new InputBindingSpec(
                    Handle("Mouse and Keyboard", "MouseDelta"),
                    [Handle("Mouse and Keyboard", "MouseLeft")],
                    [new ScaleVector2(0.25f)]),

                new InputBindingSpec(
                    Handle("Xbox Controller", "LeftStick"),
                    [],
                    [new RateVector2(120.0f)]),
            ],
        };

        public InputAction_Float Zoom { get; } = new()
        {
            Name = "Zoom",
            Bindings =
            [
                new InputBindingSpec(
                    Handle("Mouse and Keyboard", "MouseWheel"),
                    [],
                    [new ScaleFloat(0.15f)]),

                new InputBindingSpec(
                    Handle("Xbox Controller", "RightStick"),
                    [],
                    [new SwizzleY(), new RateFloat(1.5f)]),
            ],
        };

        public InputContext Context { get; }

        public EditorViewportContext()
        {
            Context = new InputContext
            {
                Name = "EditorViewport",
                Enabled = true,
                Actions = [Focus, Orbit, Zoom],
            };
        }

        private static InputValueHandle Handle(string device, string control) => new(device, control);
    }
}

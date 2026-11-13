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
                new InputBindingSpec(Handle("Mouse and Keyboard",
                        "F"),
                    [
                    ],
                    [
                    ]),
            ],
            Processors =
            [
            ],
            Triggers =
            [
            ],
        };

        public InputAction_Vector2 Orbit { get; } = new()
        {
            Name = "Orbit",
            Bindings =
            [
                new InputBindingSpec(
                    Handle("Mouse and Keyboard",
                        "MouseDelta"),
                    [
                        Handle("Mouse and Keyboard",
                            "MouseLeft")
                    ],
                    [
                        new ScaleVector2(0.25f)
                    ]),

                new InputBindingSpec(
                    Handle("Xbox Controller",
                        "LeftStick"),
                    [
                    ],
                    [
                        new DeadzoneVector2(0.15f),
                        new RateVector2(120.0f)
                    ]),
            ],
            Processors =
            [
            ],
            Triggers =
            [
            ],
        };

        public InputAction_Float Zoom { get; } = new()
        {
            Name = "Zoom",
            Bindings =
            [
                new InputBindingSpec(
                    Handle("Mouse and Keyboard",
                        "MouseWheel"),
                    [
                    ],
                    [
                        new ScaleFloat(0.15f)
                    ]),

                new InputBindingSpec(
                    Handle("Xbox Controller",
                        "RightStick"),
                    [
                    ],
                    [
                        new DeadzoneVector2(0.15f),
                        new SwizzleY(),
                        new RateFloat(1.5f)
                    ]),
            ],
            Processors =
            [
            ],
            Triggers =
            [
            ],
        };

        public InputAction_Bool Select { get; } = new()
        {
            Name = "Select",
            Bindings =
            [
                new InputBindingSpec(Handle("Mouse and Keyboard",
                        "MouseLeft"),
                    [
                    ],
                    [
                    ]),
            ],
            Processors =
            [
            ],
            Triggers =
            [
                new Press<bool>()
            ],
        };

        public InputAction_Bool ToggleSelect { get; } = new()
        {
            Name = "ToggleSelect",
            Bindings =
            [
                new InputBindingSpec(Handle("Mouse and Keyboard",
                        "MouseLeft"),
                    [
                        Handle("Mouse and Keyboard",
                            "ShiftLeft")
                    ],
                    [
                    ]),
            ],
            Processors =
            [
            ],
            Triggers =
            [
                new Press<bool>()
            ],
        };

        public InputAction_Vector2 MousePosition { get; } = new()
        {
            Name = "MousePosition",
            Bindings =
            [
                new InputBindingSpec(Handle("Mouse and Keyboard",
                        "MousePosition01"),
                    [
                    ],
                    [
                    ]),
            ],
            Processors =
            [
            ],
            Triggers =
            [
            ],
        };

        public InputContext Context { get; }

        public EditorViewportContext()
        {
            Context = new InputContext
            {
                Name = "EditorViewport",
                Enabled = true,
                Actions = [Focus, Orbit, Zoom, Select, ToggleSelect, MousePosition],
            };
        }

        private static InputValueHandle Handle(string device, string control) => new(device, control);
    }
}

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
            Controls =
            [
                new InputValueHandle("Mouse and Keyboard", "F"),
            ],
        };

        public InputAction_Float Zoom { get; } = new()
        {
            Name = "Zoom",
            Controls =
            [
                new InputValueHandle("Mouse and Keyboard", "MouseWheel"),
            ],
        };

        public InputContext Context { get; }

        public EditorViewportContext()
        {
            Context = new InputContext
            {
                Name = "EditorViewport",
                Enabled = true,
                Actions = [Focus, Zoom],
            };
        }
    }
}

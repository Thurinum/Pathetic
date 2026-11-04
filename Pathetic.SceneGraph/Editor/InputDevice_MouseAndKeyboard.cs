using System.Numerics;
using NeoVeldrid;
using NeoVeldrid.Sdl2;
using Pathetic.Input;
using Pathetic.Renderer;

namespace Pathetic.SceneGraph.Editor;

/// <summary>
/// Mouse and keyboard device fed by the SDL2 event stream of <see cref="RenderWindow"/>.
/// Keyboard and mouse buttons arrive as edges, so they are latched into held-state sets;
/// the device publishes those levels as values, and the actions derive active/edge state
/// from consecutive samples. Mouse motion and wheel are per-frame deltas.
/// </summary>
public sealed class InputDevice_MouseAndKeyboard : InputDevice, IDisposable
{
    private readonly RenderWindow _renderWindow;
    private readonly Dictionary<Key, InputValue<bool>> _keys = [];
    private readonly Dictionary<MouseButton, InputValue<bool>> _buttons = [];
    private readonly HashSet<Key> _keysDown = [];
    private readonly HashSet<MouseButton> _buttonsDown = [];

    private Sdl2Window? _window;
    private float _wheelDelta;
    private bool _disposed;

    public override string Name => "Mouse and Keyboard";
    public override Dictionary<string, IInputValue> Controls { get; } = [];

    public InputValue<Vector2> MousePosition { get; } = new();
    public InputValue<Vector2> MouseDelta { get; } = new();
    public InputValue<float> MouseWheel { get; } = new();

    public InputDevice_MouseAndKeyboard(RenderWindow renderWindow)
    {
        _renderWindow = renderWindow;

        // Key and MouseButton both contain aliases (LShift/ShiftLeft) and sentinel members
        // (LastKey/LastButton); Distinct keeps exactly one control per enum value.
        foreach (Key key in Enum.GetValues<Key>().Distinct().Where(k => k is not (Key.Unknown or Key.LastKey)))
        {
            _keys[key] = Register(key.ToString(), new InputValue<bool>());
        }

        foreach (MouseButton button in Enum.GetValues<MouseButton>().Distinct().Where(b => b != MouseButton.LastButton))
        {
            _buttons[button] = Register($"Mouse{button}", new InputValue<bool>());
        }

        Controls["MousePosition"] = MousePosition;
        Controls["MouseDelta"] = MouseDelta;
        Controls["MouseWheel"] = MouseWheel;
    }

    protected override void Poll()
    {
        Attach();

        foreach ((Key key, InputValue<bool> value) in _keys)
            value.Value = _keysDown.Contains(key);

        foreach ((MouseButton button, InputValue<bool> value) in _buttons)
            value.Value = _buttonsDown.Contains(button);

        if (_window is not null)
        {
            int x = 0, y = 0, dx = 0, dy = 0;

            // Absolute position for aiming/UI, relative motion for camera look.
            // GetRelativeMouseState returns motion since the previous call, so it must be
            // polled exactly once per frame, and it keeps working while the cursor is captured.
            Sdl2Window.SdlInstance.GetMouseState(ref x, ref y);
            Sdl2Window.SdlInstance.GetRelativeMouseState(ref dx, ref dy);

            // SDL reports motion with Y growing downward; values are published Y-up to match
            // the world convention. MousePosition stays raw window pixels, like ImGui.
            MousePosition.Value = new Vector2(x, y);
            MouseDelta.Value = new Vector2(dx, -dy);
        }

        MouseWheel.Value = _wheelDelta;
        _wheelDelta = 0f;
    }

    private void Attach()
    {
        if (_window is not null || _renderWindow.Window is not { } window)
            return;

        _window = window;
        window.KeyDown += OnKeyDown;
        window.KeyUp += OnKeyUp;
        window.MouseDown += OnMouseDown;
        window.MouseUp += OnMouseUp;
        window.MouseWheel += OnMouseWheel;
        window.FocusLost += OnFocusLost;
    }

    private void OnKeyDown(KeyEvent e) => _keysDown.Add(e.Key);
    private void OnKeyUp(KeyEvent e) => _keysDown.Remove(e.Key);
    private void OnMouseDown(MouseEvent e) => _buttonsDown.Add(e.MouseButton);
    private void OnMouseUp(MouseEvent e) => _buttonsDown.Remove(e.MouseButton);
    private void OnMouseWheel(MouseWheelEventArgs e) => _wheelDelta += e.WheelDelta;

    private void OnFocusLost()
    {
        // SDL stops delivering KeyUp once the window loses focus, so drop held state
        // or a key released while unfocused stays pressed forever.
        _keysDown.Clear();
        _buttonsDown.Clear();
    }

    private InputValue<T> Register<T>(string name, InputValue<T> value) where T : struct
    {
        Controls[name] = value;
        return value;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_window is null)
            return;

        _window.KeyDown -= OnKeyDown;
        _window.KeyUp -= OnKeyUp;
        _window.MouseDown -= OnMouseDown;
        _window.MouseUp -= OnMouseUp;
        _window.MouseWheel -= OnMouseWheel;
        _window.FocusLost -= OnFocusLost;

        _window = null;
    }
}

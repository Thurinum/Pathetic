using System.Numerics;
using Microsoft.Extensions.Logging;
using NeoVeldrid.Sdl2;
using Pathetic.Input;
using Silk.NET.SDL;

namespace Pathetic.SceneGraph.Editor;

/// <summary>
/// Xbox-compatible game controller polled through SDL2's game controller API.
/// Sticks are normalized to -1..1, Y pointing up, with a radial deadzone.
/// Opens the first controller it finds and reopens after a disconnect.
/// </summary>
public sealed unsafe partial class InputDevice_XboxController : InputDevice, IDisposable
{
    // SDL_INIT_GAMECONTROLLER. NeoVeldrid only initializes SDL_INIT_VIDEO, and the
    // game controller API is unusable until this subsystem is up.
    private const uint GameControllerSubsystem = 0x00002000;

    private const float AxisMax = 32767.0f;
    private const float Deadzone = 0.15f;

    private readonly ILogger<InputDevice_XboxController> _logger;

    private GameController* _controller;
    private bool _subsystemInitialized;
    private bool _scanLogged;

    public override string Name => "Xbox Controller";
    public override Dictionary<string, IInputValue> Snapshot { get; } = [];

    /// <summary>
    /// Raw left stick axes, -1..1, Y pointing up.
    /// </summary>
    public InputValue<Vector2> LeftStick { get; } = new();

    /// <summary>
    /// Raw right stick axes, -1..1, Y pointing up.
    /// </summary>
    public InputValue<Vector2> RightStick { get; } = new();

    public InputDevice_XboxController(ILogger<InputDevice_XboxController> logger)
    {
        _logger = logger;

        Snapshot["LeftStick"] = LeftStick;
        Snapshot["RightStick"] = RightStick;
    }

    protected override void Poll(float deltaTime)
    {
        if (!EnsureOpen())
        {
            LeftStick.Value = Vector2.Zero;
            RightStick.Value = Vector2.Zero;
            return;
        }

        LeftStick.Value = ReadStick(GameControllerAxis.Leftx, GameControllerAxis.Lefty);
        RightStick.Value = ReadStick(GameControllerAxis.Rightx, GameControllerAxis.Righty);
    }

    private Vector2 ReadStick(GameControllerAxis xAxis, GameControllerAxis yAxis)
    {
        Vector2 value = new(
            Normalize(Sdl2Window.SdlInstance.GameControllerGetAxis(_controller, xAxis)),
            -Normalize(Sdl2Window.SdlInstance.GameControllerGetAxis(_controller, yAxis)));

        return value.LengthSquared() < Deadzone * Deadzone ? Vector2.Zero : value;
    }

    private static float Normalize(short value) => Math.Clamp(value / AxisMax, -1.0f, 1.0f);

    private bool EnsureOpen()
    {
        if (!_subsystemInitialized)
        {
            _subsystemInitialized = true;

            if (Sdl2Window.SdlInstance.InitSubSystem(GameControllerSubsystem) != 0)
            {
                string error = Sdl2Window.SdlInstance.GetErrorS();
                LogSubsystemUnavailable(error);
            }
        }

        if (_controller is not null)
        {
            if (Sdl2Window.SdlInstance.GameControllerGetAttached(_controller) == SdlBool.True)
                return true;

            string name = Sdl2Window.SdlInstance.GameControllerNameS(_controller);
            LogDisconnected(name);
            Close();
        }

        int count = Sdl2Window.SdlInstance.NumJoysticks();

        for (int i = 0; i < count; i++)
        {
            bool isController = Sdl2Window.SdlInstance.IsGameController(i) == SdlBool.True;

            if (!_scanLogged)
            {
                string joystickName = Sdl2Window.SdlInstance.JoystickNameForIndexS(i);
                LogJoystick(i, joystickName, isController);
            }

            if (!isController)
                continue;

            _controller = Sdl2Window.SdlInstance.GameControllerOpen(i);

            if (_controller is not null)
            {
                string openedName = Sdl2Window.SdlInstance.GameControllerNameS(_controller);
                LogOpened(openedName);
                _scanLogged = true;
                return true;
            }

            string openError = Sdl2Window.SdlInstance.GetErrorS();
            LogOpenFailed(i, openError);
        }

        if (!_scanLogged)
        {
            _scanLogged = true;
            LogNoController(count);
        }

        return false;
    }

    private void Close()
    {
        if (_controller is null)
            return;

        Sdl2Window.SdlInstance.GameControllerClose(_controller);
        _controller = null;
    }

    public void Dispose() => Close();

    [LoggerMessage(Level = LogLevel.Information, Message = "Joystick {Index}: {Name} (game controller: {IsController})")]
    private partial void LogJoystick(int index, string name, bool isController);

    [LoggerMessage(Level = LogLevel.Information, Message = "Controller opened: {Name}")]
    private partial void LogOpened(string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Controller disconnected: {Name}")]
    private partial void LogDisconnected(string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to open controller at index {Index}: {Error}")]
    private partial void LogOpenFailed(int index, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "SDL game controller subsystem unavailable: {Error}")]
    private partial void LogSubsystemUnavailable(string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "No game controller found ({Joysticks} joystick(s) present)")]
    private partial void LogNoController(int joysticks);
}

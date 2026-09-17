// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Microsoft.Extensions.Logging;
using SDL;
using SharpKind.Input;
using static SDL.SDL3;

namespace SharpKind.SDL;

public sealed unsafe class SDLInput(ILogger? logger) : IInput, IDisposable
{
    // Kept apart because they're opened/closed by different SDL calls. A mapped device appears in both event streams, so it's only ever opened as a gamepad.
    private readonly Dictionary<SDL_JoystickID, nint> _gamepads = [];
    private readonly Dictionary<SDL_JoystickID, nint> _joysticks = [];

    // What each hat last reported, so a direction that has gone can be told
    // from one that was never down.
    private readonly Dictionary<SDL_JoystickID, byte> _hats = [];
    private IKeyboardSink? _keyboard;
    private IGamepadSink? _gamepad;
    private bool _isDisposed;

    public void Register(IKeyboardSink keyboard) => _keyboard = keyboard;

    public void Register(IGamepadSink gamepad)
    {
        SDLGuard.Execute(() => SDL_Init(SDL_InitFlags.SDL_INIT_GAMEPAD | SDL_InitFlags.SDL_INIT_JOYSTICK));
        _gamepad = gamepad;
    }

    public void Poll()
    {
        while (PollEvent(out SDL_Event sdlEvent) &&
            sdlEvent.type != (uint)SDL_EventType.SDL_EVENT_POLL_SENTINEL &&
            _keyboard?.Close != true)
        {
            switch ((SDL_EventType)sdlEvent.type)
            {
                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    (ConsoleKey key, ConsoleModifiers modifiers) = SDLHelper.KeyConverter(sdlEvent.key.key);
                    _keyboard?.KeyDown(key, modifiers);
                    break;

                case SDL_EventType.SDL_EVENT_KEY_UP:
                    (ConsoleKey key1, ConsoleModifiers modifiers1) = SDLHelper.KeyConverter(sdlEvent.key.key);
                    _keyboard?.KeyUp(key1, modifiers1);
                    break;

                case SDL_EventType.SDL_EVENT_QUIT:
                    _keyboard?.Close = true;
                    break;

                default:
                    if (_gamepad is not null)
                    {
                        HandleDeviceEvent(sdlEvent, _gamepad);
                    }

                    break;
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        foreach (nint gamepad in _gamepads.Values)
        {
            SDL_CloseGamepad((SDL_Gamepad*)gamepad);
        }

        foreach (nint joystick in _joysticks.Values)
        {
            SDL_CloseJoystick((SDL_Joystick*)joystick);
        }

        _gamepads.Clear();
        _joysticks.Clear();
        _hats.Clear();
        _isDisposed = true;
    }

    // Internal so press/release edges can be tested: a direction left down is the failure this exists to prevent.
    internal static void HatDirectionChanged(
        int deviceId, byte was, byte now, uint direction, GamepadButton button, IGamepadSink gamepad)
    {
        bool down = (now & direction) != 0;

        if (down == ((was & direction) != 0))
        {
            return;
        }

        if (down)
        {
            gamepad.ButtonDown(deviceId, button);
        }
        else
        {
            gamepad.ButtonUp(deviceId, button);
        }
    }

    // Axes and triggers normalise the same way; a digital HID stick sits at the range ends, exactly -1/0/+1.
    private static float Normalise(short value) => Math.Clamp(value / 32767f, -1f, 1f);

    private static GamepadButton ConvertGamepadButton(SDL_GamepadButton button) => button switch
    {
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH => GamepadButton.A,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST => GamepadButton.B,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST => GamepadButton.X,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH => GamepadButton.Y,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER => GamepadButton.LeftShoulder,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER => GamepadButton.RightShoulder,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK => GamepadButton.Back,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START => GamepadButton.Start,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP => GamepadButton.DPadUp,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN => GamepadButton.DPadDown,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT => GamepadButton.DPadLeft,
        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT => GamepadButton.DPadRight,
        _ => GamepadButton.None,
    };

    private static GamepadAxis? ConvertGamepadAxis(SDL_GamepadAxis axis) => axis switch
    {
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX => GamepadAxis.LeftX,
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY => GamepadAxis.LeftY,
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX => GamepadAxis.RightX,
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY => GamepadAxis.RightY,
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER => GamepadAxis.LeftTrigger,
        SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER => GamepadAxis.RightTrigger,
        _ => null,
    };

    // An unmapped device numbers its buttons/axes with no names, so index order is the only mapping there is.
    private static GamepadButton ConvertJoystickButton(byte index) => index switch
    {
        0 => GamepadButton.A,
        1 => GamepadButton.B,
        2 => GamepadButton.X,
        3 => GamepadButton.Y,
        4 => GamepadButton.LeftShoulder,
        5 => GamepadButton.RightShoulder,
        6 => GamepadButton.Back,
        7 => GamepadButton.Start,
        _ => GamepadButton.None,
    };

    private static GamepadAxis? ConvertJoystickAxis(byte index) => index switch
    {
        0 => GamepadAxis.LeftX,
        1 => GamepadAxis.LeftY,
        2 => GamepadAxis.RightX,
        3 => GamepadAxis.Throttle,
        _ => null,
    };

    private static bool PollEvent(out SDL_Event sdlEvent)
    {
        // SDL3's bool result signals whether an event was returned, not failure; no error case to guard here.
        sdlEvent = default;

        fixed (SDL_Event* eventPtr = &sdlEvent)
        {
            _ = SDL_PollEvent(eventPtr);
        }

        return true;
    }

    private static void ButtonChanged(int deviceId, GamepadButton button, bool down, IGamepadSink gamepad)
    {
        if (down)
        {
            gamepad.ButtonDown(deviceId, button);
        }
        else
        {
            gamepad.ButtonUp(deviceId, button);
        }
    }

    private static void AxisMoved(int deviceId, GamepadAxis? axis, short value, IGamepadSink gamepad)
    {
        if (axis.HasValue)
        {
            gamepad.AxisMoved(deviceId, axis.Value, Normalise(value));
        }
    }

    // For the log only, so a hat event reads next to the axis events around it. The game sees the hat as buttons.
    private static (float X, float Y) HatDirection(byte hat)
    {
        float x = 0f;
        if ((hat & SDL_HAT_LEFT) != 0)
        {
            x = -1f;
        }
        else if ((hat & SDL_HAT_RIGHT) != 0)
        {
            x = 1f;
        }

        float y = 0f;
        if ((hat & SDL_HAT_UP) != 0)
        {
            y = -1f;
        }
        else if ((hat & SDL_HAT_DOWN) != 0)
        {
            y = 1f;
        }

        return (x, y);
    }

    private void HandleDeviceEvent(SDL_Event sdlEvent, IGamepadSink gamepad)
    {
        switch ((SDL_EventType)sdlEvent.type)
        {
            case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                OpenGamepad(sdlEvent.gdevice.which, gamepad);
                break;

            case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                CloseGamepad(sdlEvent.gdevice.which, gamepad);
                break;

            case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
            case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
                GamepadButtonEvent(sdlEvent, gamepad);
                break;

            case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
                GamepadAxisEvent(sdlEvent, gamepad);
                break;

            case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
                OpenJoystick(sdlEvent.jdevice.which, gamepad);
                break;

            case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
                CloseJoystick(sdlEvent.jdevice.which, gamepad);
                break;

            case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
            case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP:
                JoystickButtonEvent(sdlEvent, gamepad);
                break;

            case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION:
                JoystickAxisEvent(sdlEvent, gamepad);
                break;

            case SDL_EventType.SDL_EVENT_JOYSTICK_HAT_MOTION:
                JoystickHatEvent(sdlEvent, gamepad);
                break;
        }
    }

    private void GamepadButtonEvent(SDL_Event sdlEvent, IGamepadSink gamepad)
    {
        bool down = sdlEvent.type == (uint)SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN;
        GamepadButton button = ConvertGamepadButton((SDL_GamepadButton)sdlEvent.gbutton.button);

        LogButton(sdlEvent.gbutton.button, down, button, sdlEvent.gbutton.which);
        ButtonChanged((int)sdlEvent.gbutton.which, button, down, gamepad);
    }

    private void GamepadAxisEvent(SDL_Event sdlEvent, IGamepadSink gamepad)
    {
        GamepadAxis? axis = ConvertGamepadAxis((SDL_GamepadAxis)sdlEvent.gaxis.axis);

        LogAxis(sdlEvent.gaxis.axis, sdlEvent.gaxis.value, axis, sdlEvent.gaxis.which);
        AxisMoved((int)sdlEvent.gaxis.which, axis, sdlEvent.gaxis.value, gamepad);
    }

    private void JoystickButtonEvent(SDL_Event sdlEvent, IGamepadSink gamepad)
    {
        if (!_joysticks.ContainsKey(sdlEvent.jbutton.which))
        {
            return;
        }

        bool down = sdlEvent.type == (uint)SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN;
        GamepadButton button = ConvertJoystickButton(sdlEvent.jbutton.button);

        LogButton(sdlEvent.jbutton.button, down, button, sdlEvent.jbutton.which);
        ButtonChanged((int)sdlEvent.jbutton.which, button, down, gamepad);
    }

    private void JoystickAxisEvent(SDL_Event sdlEvent, IGamepadSink gamepad)
    {
        if (!_joysticks.ContainsKey(sdlEvent.jaxis.which))
        {
            return;
        }

        GamepadAxis? axis = ConvertJoystickAxis(sdlEvent.jaxis.axis);

        LogAxis(sdlEvent.jaxis.axis, sdlEvent.jaxis.value, axis, sdlEvent.jaxis.which);
        AxisMoved((int)sdlEvent.jaxis.which, axis, sdlEvent.jaxis.value, gamepad);
    }

    private void JoystickHatEvent(SDL_Event sdlEvent, IGamepadSink gamepad)
    {
        if (!_joysticks.ContainsKey(sdlEvent.jhat.which))
        {
            return;
        }

        byte value = sdlEvent.jhat.value;

        if (logger?.IsEnabled(LogLevel.Debug) == true)
        {
            (float x, float y) = HatDirection(value);
            uint id = (uint)sdlEvent.jhat.which;
            SDLInputLogMessages.HatEvent(logger, value, x, y, id);
        }

        // Each direction is compared with what it was, since SDL sends no separate release event.
        byte was = _hats.TryGetValue(sdlEvent.jhat.which, out byte previous) ? previous : (byte)0;
        _hats[sdlEvent.jhat.which] = value;
        int hatDevice = (int)sdlEvent.jhat.which;

        HatDirectionChanged(hatDevice, was, value, SDL_HAT_UP, GamepadButton.DPadUp, gamepad);
        HatDirectionChanged(hatDevice, was, value, SDL_HAT_DOWN, GamepadButton.DPadDown, gamepad);
        HatDirectionChanged(hatDevice, was, value, SDL_HAT_LEFT, GamepadButton.DPadLeft, gamepad);
        HatDirectionChanged(hatDevice, was, value, SDL_HAT_RIGHT, GamepadButton.DPadRight, gamepad);
    }

    private void LogButton(byte index, bool down, GamepadButton button, SDL_JoystickID which)
    {
        if (logger?.IsEnabled(LogLevel.Debug) == true)
        {
            string state = down ? "down" : "up";
            uint id = (uint)which;
            SDLInputLogMessages.ButtonEvent(logger, index, state, button, id);
        }
    }

    private void LogAxis(byte index, short raw, GamepadAxis? axis, SDL_JoystickID which)
    {
        if (logger?.IsEnabled(LogLevel.Debug) == true)
        {
            float value = Normalise(raw);
            uint id = (uint)which;
            SDLInputLogMessages.AxisEvent(logger, index, raw, value, axis, id);
        }
    }

    private void OpenGamepad(SDL_JoystickID which, IGamepadSink gamepad)
    {
        if (_gamepads.ContainsKey(which))
        {
            return;
        }

        nint handle = (nint)SDL_OpenGamepad(which);
        if (handle == nint.Zero)
        {
            // A device that will not open is simply not available to play
            // with; failing the frame over it would be worse than ignoring it.
            return;
        }

        _gamepads[which] = handle;

        // Read whatever the logger is enabled or not: the game matches on
        // this name to pick a control profile.
        string name = SDL_GetGamepadName((SDL_Gamepad*)handle) ?? "unnamed";

        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            SDL_Joystick* joystick = SDL_GetGamepadJoystick((SDL_Gamepad*)handle);
            uint id = (uint)which;
            int axes = SDL_GetNumJoystickAxes(joystick);
            int buttons = SDL_GetNumJoystickButtons(joystick);
            int hats = SDL_GetNumJoystickHats(joystick);
            SDLInputLogMessages.GamepadConnected(logger, name, id, axes, buttons, hats);
        }

        gamepad.Connected((int)which, name);
    }

    private void CloseGamepad(SDL_JoystickID which, IGamepadSink gamepad)
    {
        if (!_gamepads.Remove(which, out nint handle))
        {
            return;
        }

        SDL_CloseGamepad((SDL_Gamepad*)handle);

        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            uint id = (uint)which;
            SDLInputLogMessages.DeviceDisconnected(logger, id);
        }

        gamepad.Disconnected((int)which);
    }

    private void OpenJoystick(SDL_JoystickID which, IGamepadSink gamepad)
    {
        // A mapped device is handled through the gamepad events, which name
        // their buttons; opening it here as well would double-count it.
        if (SDL_IsGamepad(which) || _joysticks.ContainsKey(which))
        {
            return;
        }

        nint handle = (nint)SDL_OpenJoystick(which);
        if (handle == nint.Zero)
        {
            return;
        }

        _joysticks[which] = handle;

        string name = SDL_GetJoystickName((SDL_Joystick*)handle) ?? "unnamed";

        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            SDL_Joystick* joystick = (SDL_Joystick*)handle;
            uint id = (uint)which;
            int axes = SDL_GetNumJoystickAxes(joystick);
            int buttons = SDL_GetNumJoystickButtons(joystick);
            int hats = SDL_GetNumJoystickHats(joystick);
            SDLInputLogMessages.JoystickConnected(logger, name, id, axes, buttons, hats);
        }

        gamepad.Connected((int)which, name);
    }

    private void CloseJoystick(SDL_JoystickID which, IGamepadSink gamepad)
    {
        if (!_joysticks.Remove(which, out nint handle))
        {
            return;
        }

        SDL_CloseJoystick((SDL_Joystick*)handle);

        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            uint id = (uint)which;
            SDLInputLogMessages.DeviceDisconnected(logger, id);
        }

        gamepad.Disconnected((int)which);

        // The hat's remembered state goes with the device, or a stick
        // unplugged mid-push would leave a direction down for the next one.
        _ = _hats.Remove(which);
    }
}

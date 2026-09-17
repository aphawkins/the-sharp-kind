// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Input;

namespace SharpKind.Abstraction.Controls;

/// <summary>
/// The bindings as the game asks about them: "is this action wanted", rather
/// than "is this key down". Everything that reads a control goes through
/// here, so the keyboard and whichever stick is being flown answer the same
/// question and neither knows about the other.
/// </summary>
/// <remarks>
/// Built once from <see cref="ControlBindings"/> and read every frame, so
/// the file's strings are turned into enums here and not in the game loop.
/// A controller entry is chosen per read rather than cached, because the
/// commander can plug a different stick in while the game is running.
/// </remarks>
/// <remarks>
/// Not sealed: a game is expected to derive a named type from it, because
/// the two type arguments are the same everywhere in that game and reading
/// them at every call site earns nothing.
/// </remarks>
/// <typeparam name="TAction">The game's bindable commands.</typeparam>
/// <typeparam name="TAxis">The game's controls that take a position.</typeparam>
public class ControlMap<TAction, TAxis>
    where TAction : struct, Enum
    where TAxis : struct, Enum
{
    // Past this a trigger counts as pulled. A trigger is an axis, so it has
    // no press of its own; crossing the threshold is the nearest thing.
    private const float TriggerThreshold = 0.5f;

    // Enough to clear a worn potentiometer's wander: a SideWinder Precision 2 at rest drifts up to 0.03 on its X axis.
    private const float Deadzone = 0.1f;

    // How far a stick must travel before it counts as held in a direction.
    // A digital stick sits at the ends of its range, so it passes whatever
    // this is set to.
    private const float AxisThreshold = 0.5f;

    // Which actions are "this axis, pushed that way", so a stick with no
    // position to report can still hold a direction. The game supplies it,
    // because only the game knows that RollLeft is its Roll axis going
    // negative.
    private readonly Dictionary<TAction, (TAxis Axis, int Sign)> _directions;

    private readonly Dictionary<TAction, List<ConsoleKey>> _keys = [];
    private readonly List<DeviceMap> _devices = [];
    private readonly IKeyboard _keyboard;
    private readonly IGamepad _gamepad;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlMap{TAction, TAxis}"/>
    /// class for a game whose controls are all presses.
    /// </summary>
    /// <param name="bindings">The file's bindings.</param>
    /// <param name="keyboard">Where key state is read from.</param>
    /// <param name="gamepad">Where controller state is read from.</param>
    public ControlMap(ControlBindings bindings, IKeyboard keyboard, IGamepad gamepad)
        : this(bindings, keyboard, gamepad, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlMap{TAction, TAxis}"/>
    /// class for a game with flight axes as well.
    /// </summary>
    /// <param name="bindings">The file's bindings.</param>
    /// <param name="keyboard">Where key state is read from.</param>
    /// <param name="gamepad">Where controller state is read from.</param>
    /// <param name="directions">
    /// Which actions mean "this axis, pushed that way", with -1 for the
    /// negative end. It is what lets a digital stick - which has no position
    /// to report - hold a direction, so a game with axes should pass it.
    /// </param>
    public ControlMap(
        ControlBindings bindings,
        IKeyboard keyboard,
        IGamepad gamepad,
        IReadOnlyDictionary<TAction, (TAxis Axis, int Sign)>? directions)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        _keyboard = keyboard;
        _gamepad = gamepad;
        _directions = directions is null ? [] : new(directions.ToDictionary(d => d.Key, d => d.Value));

        foreach ((string action, List<string> keys) in bindings.Keyboard)
        {
            if (!Enum.TryParse(action, ignoreCase: true, out TAction parsed))
            {
                continue;
            }

            _keys[parsed] = [.. keys
                .Select(key => Enum.TryParse(key, ignoreCase: true, out ConsoleKey k) ? k : (ConsoleKey?)null)
                .Where(key => key.HasValue)
                .Select(key => key!.Value)];
        }

        foreach (ControllerBindings controller in bindings.Controllers)
        {
            _devices.Add(new DeviceMap(controller));
        }
    }

    // The entry naming the device being flown, or the catch-all. Resolved per read, so swapping sticks mid-game just works.
    private DeviceMap? Active
    {
        get
        {
            if (!_gamepad.IsConnected)
            {
                return null;
            }

            string name = _gamepad.DeviceName;
            DeviceMap? fallback = null;

            foreach (DeviceMap device in _devices)
            {
                if (device.IsAnyDevice)
                {
                    fallback = device;
                }
                else if (name.Contains(device.Device, StringComparison.OrdinalIgnoreCase))
                {
                    return device;
                }
            }

            return fallback;
        }
    }

    /// <summary>
    /// Whether the control is down now: a key held, a button held, or a
    /// trigger past its threshold. For controls polled every frame.
    /// </summary>
    public bool IsHeld(TAction action) => KeyHeld(action) || DeviceHolds(action);

    /// <summary>
    /// Whether a bound <em>key</em> is down, ignoring whatever the stick is
    /// doing.
    /// </summary>
    /// <remarks>
    /// For the one question that has to tell the two apart: an analog stick
    /// sets the rate of turn outright, and it must not do that while the
    /// commander is flying on the keys. Asking <see cref="IsHeld"/> there
    /// would answer yes to the stick's own axis and the stick would suppress
    /// itself.
    /// </remarks>
    public bool IsKeyHeld(TAction action) => KeyHeld(action);

    /// <summary>
    /// Whether the control was pressed this update, consumed once. For the
    /// commands that must act once per press.
    /// </summary>
    /// <remarks>
    /// A trigger cannot answer this - it is an axis with no press to
    /// consume - so a caller that binds one uses <see cref="IsHeld"/> and
    /// makes the edge itself.
    /// </remarks>
    public bool WasPressed(TAction action)
    {
        // Both sides read, neither short-circuits: skipping one would leave its one-shot press to fire on the next update.
        bool key = KeyPressed(action);
        bool button = DevicePressed(action);

        return key || button;
    }

    /// <summary>
    /// How far the flight axis is pushed, -1 to +1, with the deadzone taken
    /// out and the rest stretched back over the full range. Zero when the
    /// axis is unbound, or belongs to a device that cannot report a
    /// position - which is what makes the caller fall through to the keys.
    /// </summary>
    public float Deflection(TAxis axis)
    {
        if (Active?.Axes.TryGetValue(axis, out (GamepadAxis Axis, bool Invert) binding) != true)
        {
            return 0;
        }

        if (!_gamepad.IsAnalog(binding.Axis))
        {
            return 0;
        }

        float value = _gamepad.Axis(binding.Axis);
        float magnitude = MathF.Abs(value);

        if (magnitude <= Deadzone)
        {
            return 0;
        }

        float scaled = MathF.CopySign((magnitude - Deadzone) / (1 - Deadzone), value);

        return binding.Invert ? -scaled : scaled;
    }

    /// <summary>
    /// Whether the active device can report a position for this flight axis
    /// at all, which is what decides between setting the rate outright and
    /// climbing the ramp.
    /// </summary>
    public bool IsAnalog(TAxis axis)
        => Active?.Axes.TryGetValue(axis, out (GamepadAxis Axis, bool Invert) binding) == true
            && _gamepad.IsAnalog(binding.Axis);

    /// <summary>
    /// Where a throttle lever is set, 0 at the back through 1 fully
    /// forward, or null where the active device has no lever bound to it.
    /// </summary>
    /// <param name="axis">The game's throttle axis.</param>
    /// <returns>The lever's position, or null if the device has none.</returns>
    public float? Throttle(TAxis axis)
    {
        if (!IsAnalog(axis))
        {
            return null;
        }

        // SDL reports a lever pushed forward as negative and forward is
        // fast, so the range is flipped as well as halved.
        return ((1 - Deflection(axis)) / 2) switch
        {
            < 0 => 0,
            > 1 => 1,
            float value => value,
        };
    }

    private bool KeyHeld(TAction action)
    {
        if (!_keys.TryGetValue(action, out List<ConsoleKey>? keys))
        {
            return false;
        }

        foreach (ConsoleKey key in keys)
        {
            if (_keyboard.IsHeld(key))
            {
                return true;
            }
        }

        return false;
    }

    private bool KeyPressed(TAction action)
    {
        if (!_keys.TryGetValue(action, out List<ConsoleKey>? keys))
        {
            return false;
        }

        // Every bound key is read, not just until one answers: IsPressed
        // consumes, and an unread one would fire on the next update.
        bool pressed = false;

        foreach (ConsoleKey key in keys)
        {
            pressed |= _keyboard.IsPressed(key);
        }

        return pressed;
    }

    private bool DeviceHolds(TAction action)
    {
        DeviceMap? active = Active;

        return active is not null
            && ((active.Buttons.TryGetValue(action, out GamepadButton button) && _gamepad.IsHeld(button))
                || (active.Triggers.TryGetValue(action, out GamepadAxis trigger)
                    && _gamepad.Axis(trigger) >= TriggerThreshold)
                || AxisPushed(active, action));
    }

    // How a digital stick flies at all - it has no position, so the ramp is all it has. An analog one just reaches the threshold on the way over.
    private bool AxisPushed(DeviceMap active, TAction action)
    {
        if (!_directions.TryGetValue(action, out (TAxis Axis, int Sign) direction)
            || !active.Axes.TryGetValue(direction.Axis, out (GamepadAxis Axis, bool Invert) binding))
        {
            return false;
        }

        float value = _gamepad.Axis(binding.Axis);

        if (binding.Invert)
        {
            value = -value;
        }

        return direction.Sign < 0 ? value <= -AxisThreshold : value >= AxisThreshold;
    }

    private bool DevicePressed(TAction action)
        => Active?.Buttons.TryGetValue(action, out GamepadButton button) == true
            && _gamepad.IsPressed(button);

    // One controller entry with its strings already turned into enums.
    private sealed class DeviceMap
    {
        internal DeviceMap(ControllerBindings bindings)
        {
            Device = bindings.Device;
            IsAnyDevice = string.Equals(bindings.Device, ControlBindings.AnyDevice, StringComparison.Ordinal);

            foreach ((string name, AxisBinding binding) in bindings.Axes)
            {
                if (Enum.TryParse(name, ignoreCase: true, out TAxis axis)
                    && Enum.TryParse(binding.Axis, ignoreCase: true, out GamepadAxis device))
                {
                    Axes[axis] = (device, binding.Invert);
                }
            }

            foreach ((string name, string value) in bindings.Buttons)
            {
                if (Enum.TryParse(name, ignoreCase: true, out TAction action)
                    && Enum.TryParse(value, ignoreCase: true, out GamepadButton button))
                {
                    Buttons[action] = button;
                }
            }

            foreach ((string name, string value) in bindings.Triggers)
            {
                if (Enum.TryParse(name, ignoreCase: true, out TAction action)
                    && Enum.TryParse(value, ignoreCase: true, out GamepadAxis trigger))
                {
                    Triggers[action] = trigger;
                }
            }
        }

        internal string Device { get; }

        internal bool IsAnyDevice { get; }

        internal Dictionary<TAxis, (GamepadAxis Axis, bool Invert)> Axes { get; } = [];

        internal Dictionary<TAction, GamepadButton> Buttons { get; } = [];

        internal Dictionary<TAction, GamepadAxis> Triggers { get; } = [];
    }
}

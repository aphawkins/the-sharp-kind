// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Input;

/// <summary>
/// Keeps what every attached device is doing, and answers for one of them:
/// the active device, which <see cref="PreferredDevice"/> selects.
/// </summary>
/// <remarks>
/// State is per device rather than merged. Merged, two attached sticks both
/// flew the ship, and a second device resting off-centre fought the one in
/// the commander's hand.
/// </remarks>
public class SoftwareGamepad : IGamepad, IGamepadSink
{
    private readonly Dictionary<int, Device> _devices = [];

    // Arrival order, which is what "the first attached" means and what the
    // settings screen lists. A dictionary does not promise an order.
    private readonly List<int> _arrivals = [];
    private readonly IInput _input;

    public SoftwareGamepad(IInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        input.Register(this);
        _input = input;
    }

    public bool IsConnected => _arrivals.Count > 0;

    public string? PreferredDevice { get; set; }

    public string DeviceName => Active?.Name ?? string.Empty;

    public IReadOnlyList<string> AttachedDevices
        => [.. _arrivals.Select(id => _devices[id].Name)];

    // The preferred device if attached, else whatever arrived first. Resolved per read so plugging in the preferred stick mid-game picks it up.
    private Device? Active
    {
        get
        {
            if (_arrivals.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(PreferredDevice))
            {
                foreach (int id in _arrivals)
                {
                    if (string.Equals(_devices[id].Name, PreferredDevice, StringComparison.OrdinalIgnoreCase))
                    {
                        return _devices[id];
                    }
                }
            }

            return _devices[_arrivals[0]];
        }
    }

    public void ClearPressed()
    {
        foreach (Device device in _devices.Values)
        {
            device.ClearInput();
        }
    }

    public bool IsPressed(GamepadButton button)
        => button != GamepadButton.None && Active?.PressedButtons.Remove(button) == true;

    public bool IsHeld(GamepadButton button)
        => button != GamepadButton.None && Active?.HeldButtons.Contains(button) == true;

    public float Axis(GamepadAxis axis)
        => Active is { } active && active.Axes.TryGetValue(axis, out float value) ? value : 0f;

    public bool IsAnalog(GamepadAxis axis) => Active?.AnalogAxes.Contains(axis) == true;

    public void Connected(int deviceId, string name)
    {
        if (_devices.ContainsKey(deviceId))
        {
            return;
        }

        _devices[deviceId] = new Device(name ?? string.Empty);
        _arrivals.Add(deviceId);
    }

    public void Disconnected(int deviceId)
    {
        // Everything the device knew goes with it - a button held as it's unplugged would otherwise stay held forever.
        if (_devices.Remove(deviceId))
        {
            _ = _arrivals.Remove(deviceId);
        }
    }

    public void ButtonDown(int deviceId, GamepadButton button)
    {
        if (button == GamepadButton.None || !_devices.TryGetValue(deviceId, out Device? device))
        {
            return;
        }

        _ = device.HeldButtons.Add(button);
        _ = device.PressedButtons.Add(button);
    }

    public void ButtonUp(int deviceId, GamepadButton button)
    {
        if (!_devices.TryGetValue(deviceId, out Device? device))
        {
            return;
        }

        _ = device.HeldButtons.Remove(button);
        _ = device.PressedButtons.Remove(button);
    }

    public void AxisMoved(int deviceId, GamepadAxis axis, float value)
    {
        if (!_devices.TryGetValue(deviceId, out Device? device))
        {
            return;
        }

        float clamped = Math.Clamp(value, -1f, 1f);

        // A digital stick can only send an end of the range or the centre; anything in between is a potentiometer.
        if (clamped is not (-1f or 0f or 1f))
        {
            _ = device.AnalogAxes.Add(axis);
        }

        device.Axes[axis] = clamped;
    }

    public void Poll() => _input.Poll();

    private sealed class Device(string name)
    {
        internal string Name { get; } = name;

        internal HashSet<GamepadButton> HeldButtons { get; } = [];

        internal HashSet<GamepadButton> PressedButtons { get; } = [];

        internal Dictionary<GamepadAxis, float> Axes { get; } = [];

        // Not cleared with the input: device kind isn't state that goes stale between frames. Unplugging drops the whole device instead.
        internal HashSet<GamepadAxis> AnalogAxes { get; } = [];

        internal void ClearInput()
        {
            HeldButtons.Clear();
            PressedButtons.Clear();
            Axes.Clear();
        }
    }
}

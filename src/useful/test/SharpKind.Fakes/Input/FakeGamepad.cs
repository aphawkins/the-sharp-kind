// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Input;

namespace SharpKind.Fakes.Input;

// Minimal in-test fake implementation of IGamepad, tracking state per
// device as SoftwareGamepad does, so a test can attach two sticks and check
// that only the active one flies the ship.
//
// Every write also has a one-device shorthand, which is what nearly every
// test wants: it addresses a single implicit device and attaches it on
// first use, so a test about buttons does not have to talk about devices.
public sealed class FakeGamepad : IGamepad, IGamepadSink
{
    private readonly Dictionary<int, Device> _devices = [];
    private readonly List<int> _arrivals = [];

    // The device the shorthand writes to. Any id would do; this one is
    // named so the shorthand and the explicit calls can be mixed.
    public static int DefaultDeviceId => 0;

    public bool IsConnected => _arrivals.Count > 0;

    public string? PreferredDevice { get; set; }

    public string DeviceName => Active?.Name ?? string.Empty;

    public IReadOnlyList<string> AttachedDevices => [.. _arrivals.Select(id => _devices[id].Name)];

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
        => button != GamepadButton.None && Active?.Pressed.Remove(button) == true;

    public bool IsHeld(GamepadButton button)
        => button != GamepadButton.None && Active?.Held.Contains(button) == true;

    public float Axis(GamepadAxis axis)
        => Active is { } active && active.Axes.TryGetValue(axis, out float value) ? value : 0f;

    public bool IsAnalog(GamepadAxis axis) => Active?.Analog.Contains(axis) == true;

    /// <summary>Names the one implicit device, as <c>Connected</c> does.</summary>
    public void Connected(string name) => Connected(DefaultDeviceId, name);

    public void Connected(int deviceId, string name)
    {
        if (_devices.ContainsKey(deviceId))
        {
            return;
        }

        _devices[deviceId] = new Device(name ?? string.Empty);
        _arrivals.Add(deviceId);
    }

    /// <inheritdoc cref="Connected(string)"/>
    public void Disconnected() => Disconnected(DefaultDeviceId);

    public void Disconnected(int deviceId)
    {
        if (_devices.Remove(deviceId))
        {
            _ = _arrivals.Remove(deviceId);
        }
    }

    /// <inheritdoc cref="Connected(string)"/>
    public void ButtonDown(GamepadButton button) => ButtonDown(DefaultDeviceId, button);

    public void ButtonDown(int deviceId, GamepadButton button)
    {
        if (button == GamepadButton.None)
        {
            return;
        }

        Device device = Attached(deviceId);
        _ = device.Held.Add(button);
        _ = device.Pressed.Add(button);
    }

    /// <inheritdoc cref="Connected(string)"/>
    public void ButtonUp(GamepadButton button) => ButtonUp(DefaultDeviceId, button);

    public void ButtonUp(int deviceId, GamepadButton button)
    {
        Device device = Attached(deviceId);
        _ = device.Held.Remove(button);
        _ = device.Pressed.Remove(button);
    }

    /// <inheritdoc cref="Connected(string)"/>
    public void AxisMoved(GamepadAxis axis, float value) => AxisMoved(DefaultDeviceId, axis, value);

    public void AxisMoved(int deviceId, GamepadAxis axis, float value)
    {
        Device device = Attached(deviceId);
        float clamped = Math.Clamp(value, -1f, 1f);

        if (clamped is not (-1f or 0f or 1f))
        {
            _ = device.Analog.Add(axis);
        }

        device.Axes[axis] = clamped;
    }

    public void Poll()
    {
        // No-op for the fake. Real implementations may update internal state here.
    }

    // A write to a device nothing has announced attaches it unnamed, so a
    // test can press a button without first staging a connection. The real
    // gamepad ignores such a write, because SDL always opens a device
    // before it reports anything the device did.
    private Device Attached(int deviceId)
    {
        if (!_devices.TryGetValue(deviceId, out Device? device))
        {
            Connected(deviceId, string.Empty);
            device = _devices[deviceId];
        }

        return device;
    }

    private sealed class Device(string name)
    {
        internal string Name { get; } = name;

        internal HashSet<GamepadButton> Held { get; } = [];

        internal HashSet<GamepadButton> Pressed { get; } = [];

        internal Dictionary<GamepadAxis, float> Axes { get; } = [];

        internal HashSet<GamepadAxis> Analog { get; } = [];

        internal void ClearInput()
        {
            Held.Clear();
            Pressed.Clear();
            Axes.Clear();
        }
    }
}

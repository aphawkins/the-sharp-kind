// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Input;

namespace SharpKind.Fakes.Input;

// Minimal in-test fake implementation of IGamepad. Pressed and held are
// tracked apart, as SoftwareGamepad tracks them, so a one-shot menu action
// and a per-tick driving control read the same button correctly.
public sealed class FakeGamepad : IGamepad, IGamepadSink
{
    private readonly HashSet<GamepadButton> _heldButtons = [];
    private readonly HashSet<GamepadButton> _pressedButtons = [];
    private readonly Dictionary<GamepadAxis, float> _axes = [];
    private readonly HashSet<GamepadAxis> _analogAxes = [];
    private int _deviceCount;

    public bool IsConnected => _deviceCount > 0;

    public string DeviceName { get; set; } = string.Empty;

    public void ClearPressed()
    {
        _heldButtons.Clear();
        _pressedButtons.Clear();
        _axes.Clear();
    }

    public bool IsPressed(GamepadButton button)
        => button != GamepadButton.None && _pressedButtons.Remove(button);

    public bool IsHeld(GamepadButton button)
        => button != GamepadButton.None && _heldButtons.Contains(button);

    public float Axis(GamepadAxis axis) => _axes.TryGetValue(axis, out float value) ? value : 0f;

    public bool IsAnalog(GamepadAxis axis) => _analogAxes.Contains(axis);

    public void Connected(string name)
    {
        _deviceCount++;
        DeviceName = name ?? string.Empty;
    }

    public void Disconnected()
    {
        if (_deviceCount > 0)
        {
            _deviceCount--;
        }

        if (_deviceCount == 0)
        {
            ClearPressed();
            _analogAxes.Clear();
            DeviceName = string.Empty;
        }
    }

    public void ButtonDown(GamepadButton button)
    {
        if (button == GamepadButton.None)
        {
            return;
        }

        _ = _heldButtons.Add(button);
        _ = _pressedButtons.Add(button);
    }

    public void ButtonUp(GamepadButton button)
    {
        _ = _heldButtons.Remove(button);
        _ = _pressedButtons.Remove(button);
    }

    public void AxisMoved(GamepadAxis axis, float value)
    {
        float clamped = Math.Clamp(value, -1f, 1f);

        if (clamped is not (-1f or 0f or 1f))
        {
            _ = _analogAxes.Add(axis);
        }

        _axes[axis] = clamped;
    }

    public void Poll()
    {
        // No-op for the fake. Real implementations may update internal state here.
    }
}

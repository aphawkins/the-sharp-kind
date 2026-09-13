// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Input;

/// <summary>
/// Producer-facing side of the gamepad: written to by input backends (e.g.
/// <c>SDLInput</c>) as raw device events arrive. Game code should depend on
/// <see cref="IGamepad"/> instead, so it can't inject button events.
/// </summary>
public interface IGamepadSink
{
    /// <summary>
    /// A device has arrived, named as the driver names it. The name is what
    /// picks a control profile, so it is carried here rather than left to
    /// the backend to keep.
    /// </summary>
    public void Connected(string name);

    public void Disconnected();

    public void ButtonDown(GamepadButton button);

    public void ButtonUp(GamepadButton button);

    public void AxisMoved(GamepadAxis axis, float value);
}

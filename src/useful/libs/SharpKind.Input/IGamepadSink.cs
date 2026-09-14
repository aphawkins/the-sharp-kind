// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Input;

/// <summary>
/// Producer-facing side of the gamepad: written to by input backends (e.g.
/// <c>SDLInput</c>) as raw device events arrive. Game code should depend on
/// <see cref="IGamepad"/> instead, so it can't inject button events.
/// </summary>
/// <remarks>
/// Every call names the device it came from, because more than one can be
/// attached and only one of them is being flown. The backend reports what
/// each device did and takes no view on which that is; choosing is
/// <see cref="IGamepad.PreferredDevice"/>'s job.
/// </remarks>
public interface IGamepadSink
{
    /// <summary>
    /// A device has arrived, named as its driver names it. The name is what
    /// picks a control profile and what the commander selects it by; the id
    /// is what the backend's later events carry.
    /// </summary>
    public void Connected(int deviceId, string name);

    public void Disconnected(int deviceId);

    public void ButtonDown(int deviceId, GamepadButton button);

    public void ButtonUp(int deviceId, GamepadButton button);

    public void AxisMoved(int deviceId, GamepadAxis axis, float value);
}

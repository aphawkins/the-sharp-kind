// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Input;

/// <summary>
/// Consumer-facing side of the gamepad, with the same pressed-vs-held
/// semantics <see cref="IKeyboard"/> documents. Covers both target device
/// classes: XInput-style pads with analog axes, and generic-HID digital
/// sticks whose 8-way stick and hat report axes as exactly -1, 0 or +1.
/// </summary>
public interface IGamepad
{
    /// <summary>
    /// Gets a value indicating whether any device is currently attached.
    /// Nothing is pressed and every axis reads 0 while this is <c>false</c>,
    /// so callers only need this to decide whether to show gamepad hints.
    /// </summary>
    public bool IsConnected { get; }

    /// <summary>
    /// Gets the attached device's name as its driver reports it, or empty when
    /// nothing is attached. Games match on it to pick a control profile,
    /// since one stick's button 1 is another's button 4.
    /// </summary>
    /// <remarks>
    /// This is the active device's name - the one being flown. Reads below
    /// answer for that device alone.
    /// </remarks>
    public string DeviceName { get; }

    /// <summary>
    /// Gets every attached device's name, in the order they arrived. What a
    /// settings screen offers, and empty when nothing is attached.
    /// </summary>
    public IReadOnlyList<string> AttachedDevices { get; }

    /// <summary>
    /// Gets or sets the device the commander wants to fly, by name.
    /// </summary>
    /// <remarks>
    /// Null or blank takes the first attached device, and so does a name
    /// that is not attached - a stick left at home should not leave the
    /// commander with no stick at all. Setting it never fails: a name is a
    /// wish about whatever may be plugged in later, not an assertion that
    /// it is plugged in now.
    /// </remarks>
    public string? PreferredDevice { get; set; }

    public void ClearPressed();

    /// <summary>
    /// One-shot "was this button just pressed" check: a single physical
    /// button-down is consumed (and reported) at most once, even while the
    /// button remains held. Suited to menu/UI actions. For continuous
    /// controls polled every tick, use <see cref="IsHeld(GamepadButton)"/>.
    /// </summary>
    public bool IsPressed(GamepadButton button);

    /// <summary>
    /// Continuous "is this button currently down" check, with no consuming
    /// side effect, so repeated polls see the button stay held for as long
    /// as it physically is.
    /// </summary>
    public bool IsHeld(GamepadButton button);

    /// <summary>
    /// The axis position, -1 to +1 (triggers: 0 to +1). A digital device
    /// reports only -1, 0 or +1. Reading an axis never consumes it.
    /// </summary>
    public float Axis(GamepadAxis axis);

    /// <summary>
    /// Whether this axis has been seen to report a position between its
    /// extremes, which only an analog device can do.
    /// </summary>
    /// <remarks>
    /// Per axis rather than per device, because one stick is routinely both:
    /// a SideWinder's twist is analog while its hat - which this port feeds
    /// into the same axes as the stick - reports nothing but -1, 0 and +1.
    /// It answers <c>false</c> until the axis has actually moved, so a
    /// caller gets the digital answer for an axis nothing has touched yet,
    /// and it never reverts: an intermediate reading is proof that stands,
    /// where a run of extremes is only the stick being at an end.
    /// </remarks>
    public bool IsAnalog(GamepadAxis axis);

    public void Poll();
}

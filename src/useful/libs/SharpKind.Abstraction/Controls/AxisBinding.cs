// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Abstraction.Controls;

/// <summary>
/// Which of a device's axes drives a flight control, and which way round.
/// </summary>
/// <remarks>
/// Whether the axis is analog is not here, and is not a setting: it is a
/// fact about the hardware, settled by watching what the axis reports. A
/// digital stick keeps the ramp and an analog one sets the rate outright,
/// and no file should be able to claim otherwise.
/// </remarks>
public sealed class AxisBinding
{
    /// <summary>
    /// Gets or sets the device axis, named as <c>GamepadAxis</c> names it.
    /// </summary>
    public string Axis { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether pushing the axis one way
    /// should drive the control the other.
    /// </summary>
    /// <remarks>
    /// This is about the device, not about the game: it says the axis reads
    /// backwards from how the control is labelled, which is a thing about
    /// one commander's hardware. The game's own conventions - that a roll
    /// to the right is a negative rate, that a throttle pushed forward is
    /// fast - are not expressed here, or a commander correcting a backwards
    /// stick would have to know them. Off for every shipped binding.
    /// </remarks>
    public bool Invert { get; set; }
}

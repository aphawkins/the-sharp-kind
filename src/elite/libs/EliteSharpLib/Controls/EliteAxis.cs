// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

namespace EliteSharpLib.Controls;

/// <summary>
/// The flight controls that can take a position rather than a press. Only a
/// device with a stick or a lever binds these; the keyboard reaches the same
/// controls through <see cref="EliteAction"/>'s directions.
/// </summary>
internal enum EliteAxis
{
    /// <summary>
    /// No axis. Zero because the bindings file takes an enum's zero value to
    /// mean "nothing", and never a binding.
    /// </summary>
    None = 0,

    Roll = 1,
    Pitch = 2,
    Yaw = 3,

    /// <summary>
    /// A throttle lever, which sets the speed outright rather than nudging
    /// it. Unbound on a device with no lever, where speed stays on
    /// <see cref="EliteAction.SpeedUp"/> and <see cref="EliteAction.SlowDown"/>.
    /// </summary>
    Speed = 4,
}

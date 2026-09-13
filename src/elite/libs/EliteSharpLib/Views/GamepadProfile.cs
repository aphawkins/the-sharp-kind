// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Input;

namespace EliteSharpLib.Views;

/// <summary>
/// Which control layout a device gets. One shared layout cannot serve all
/// three: the same button means a different thing on each.
/// </summary>
/// <remarks>
/// <para>
/// Button 1 is the clearest case - it fires the laser on a SideWinder,
/// fires a missile on a Competition Pro, and slows the ship on an Xbox pad.
/// There is no assignment of <see cref="GamepadButton.A"/> that satisfies
/// all three, so the device has to be known before its buttons can be read.
/// </para>
/// <para>
/// Matched on the device's name, because nothing cheaper separates them: a
/// Competition Pro and an Xbox pad both arrive as mapped SDL gamepads, so
/// "joystick or pad" does not tell them apart. An unrecognised device gets
/// <see cref="Standard"/>, which is the pad layout.
/// </para>
/// </remarks>
internal enum GamepadProfile
{
    /// <summary>
    /// The XInput-style layout, and the fallback for anything unrecognised.
    /// Triggers fire, because a pad has them and a stick does not.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Microsoft SideWinder Precision 2: a raw HID flight stick with a
    /// twist, an 8-way hat, a throttle lever and 8 buttons. The only one of
    /// the three with an axis to spare for speed.
    /// </summary>
    SideWinder = 1,

    /// <summary>
    /// Speedlink Competition Pro Extra, which enumerates as "STK-7024X". A
    /// digital stick with four buttons and nothing else - no hat, no
    /// throttle - so all four are spent on the controls that matter most.
    /// </summary>
    CompetitionPro = 2,
}

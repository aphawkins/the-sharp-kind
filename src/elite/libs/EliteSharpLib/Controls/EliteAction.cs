// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

namespace EliteSharpLib.Controls;

/// <summary>
/// Everything a commander can bind a key or a button to. One name for each,
/// which is what stops the keyboard and the sticks drifting apart: both
/// halves of <c>elite.controls.sharp</c> bind to these, and a control that
/// is not here cannot be rebound.
/// </summary>
/// <remarks>
/// <para>
/// The cockpit and the view screens only. Menu navigation, Enter and the
/// typed commander names stay in code deliberately - they are how a player
/// gets out of a bad binding, and a file that could rebind them could lock
/// the game shut.
/// </para>
/// <para>
/// The flight directions are here as well as the axes
/// (<see cref="EliteAxis"/>) because a key has no position: holding one is
/// how a keyboard or a digital stick says "roll left", where an analog
/// stick says how far.
/// </para>
/// </remarks>
internal enum EliteAction
{
    None = 0,

    RollLeft = 1,
    RollRight = 2,
    PitchUp = 3,
    PitchDown = 4,
    YawLeft = 5,
    YawRight = 6,
    SpeedUp = 7,
    SlowDown = 8,

    FireLaser = 9,
    FireMissile = 10,
    TargetMissile = 11,
    UntargetMissile = 12,
    Ecm = 13,
    EnergyBomb = 14,
    EscapeCapsule = 15,

    WarpJump = 16,
    Hyperspace = 17,
    GalacticHyperspace = 18,

    /// <summary>
    /// Engage the docking computer, as the keyboard's C does.
    /// </summary>
    DockingComputerOn = 19,

    /// <inheritdoc cref="DockingComputerOn"/>
    DockingComputerOff = 20,

    /// <summary>
    /// Both halves at once: engage if the autopilot is idle, disengage if it
    /// is flying. A stick has one button to spare where the keyboard has two
    /// keys, and which half is meant is never in doubt.
    /// </summary>
    DockingComputerToggle = 21,

    Pause = 22,

    FrontView = 23,
    RearView = 24,
    LeftView = 25,
    RightView = 26,

    GalacticChart = 27,
    ShortRangeChart = 28,
    PlanetData = 29,
    MarketPrices = 30,
    CommanderStatus = 31,
    Inventory = 32,
    Options = 33,
}

// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Input;

/// <summary>
/// The buttons a game can ask about. Named after the XInput-style layout,
/// because that is the layout the mappings are written against; a generic
/// HID stick reports its fire buttons as <see cref="A"/> onwards, in the
/// order the device numbers them.
/// </summary>
public enum GamepadButton
{
    None = 0,
    A = 1,
    B = 2,
    X = 3,
    Y = 4,
    LeftShoulder = 5,
    RightShoulder = 6,
    Back = 7,
    Start = 8,

    /// <summary>
    /// A hat's four directions, reported as buttons rather than as an axis
    /// because a hat is switches and what reads it wants a press, not a
    /// position. A joystick's hat and a pad's D-pad both arrive here.
    /// </summary>
    DPadUp = 9,

    /// <inheritdoc cref="DPadUp"/>
    DPadDown = 10,

    /// <inheritdoc cref="DPadUp"/>
    DPadLeft = 11,

    /// <inheritdoc cref="DPadUp"/>
    DPadRight = 12,
}

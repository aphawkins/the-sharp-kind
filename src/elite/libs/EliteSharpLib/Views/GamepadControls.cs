// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Input;

namespace EliteSharpLib.Views;

// "Press fire to continue" prompts, deliberately not a ControlMap binding: they are the first
// thing a player meets, and a stick the controls file has no entry for should still get past them.
internal static class GamepadControls
{
    // One-shot, not held: held would skip straight through the screen behind it. Every button
    // is read rather than stopping at the first, since IsPressed consumes and would miss one.
    internal static bool WasFirePressed(IGamepad gamepad)
    {
        ArgumentNullException.ThrowIfNull(gamepad);

        bool pressed = false;

        foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
        {
            if (button != GamepadButton.None)
            {
                pressed |= gamepad.IsPressed(button);
            }
        }

        return pressed;
    }
}

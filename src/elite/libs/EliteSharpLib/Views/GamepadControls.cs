// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Input;

namespace EliteSharpLib.Views;

// What is left of the gamepad once the flight controls became bindings:
// the "press fire to continue" prompts, which are not a binding and should
// not be one.
//
// Everything a commander can rebind goes through ControlMap and
// elite.controls.sharp. These prompts do not, deliberately - they are the
// first thing a player meets, and a stick the file has no entry for should
// still get past the title screen.
internal static class GamepadControls
{
    // Any button at all, as a one-shot. Held-based would fire again on
    // every tick the button stayed down and skip straight through the
    // screen behind it.
    //
    // Every button is read rather than stopping at the first: IsPressed
    // consumes, and one left unread would fire on the next update.
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

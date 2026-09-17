// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.Input;
using StuntCarRacerSharpLib.Cars;

namespace StuntCarRacerSharpLib.Screens;

// Physics only knows a fixed left/right steer, so a threshold is all that's needed; a digital HID
// stick sits at the range ends, so it always passes it.
internal static class GamepadControls
{
    private const float Threshold = 0.5f;

    // -1 steers left, +1 right, 0 is centred.
    internal static int Steer(IGamepad gamepad)
    {
        float x = gamepad.Axis(GamepadAxis.LeftX);

        return x <= -Threshold ? -1 : x >= Threshold ? 1 : 0;
    }

    // XInput drives as ptitSeb's remake maps it (Car_Behaviour.cpp:793-817); a one-stick joystick drives the arcade way instead.
    // Both layouts are read unconditionally, since neither can reach the other's controls.
    internal static CarInput ReadCarInput(IGamepad gamepad)
    {
        CarInput input = CarInput.None;

        switch (Steer(gamepad))
        {
            case -1:
                input |= CarInput.Left;
                break;

            case 1:
                input |= CarInput.Right;
                break;
        }

        // SDL's Y axis is positive downwards, so forward on the stick is negative - same convention as the screen.
        float y = gamepad.Axis(GamepadAxis.LeftY);

        if (IsPulled(gamepad, GamepadAxis.RightTrigger) || y <= -Threshold)
        {
            input |= CarInput.Accelerate;
        }

        // Buttons 1 and 3 as the device numbers them - thumb and trigger finger on a Competition Pro.
        if (gamepad.IsHeld(GamepadButton.A) || gamepad.IsHeld(GamepadButton.X))
        {
            input |= CarInput.Boost;
        }

        if (gamepad.IsHeld(GamepadButton.B) || IsPulled(gamepad, GamepadAxis.LeftTrigger) || y >= Threshold)
        {
            input &= ~CarInput.Accelerate;
            input |= CarInput.Brake;
        }

        return input;
    }

    private static bool IsPulled(IGamepad gamepad, GamepadAxis axis) => gamepad.Axis(axis) >= Threshold;
}

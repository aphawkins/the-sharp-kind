// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Input;

namespace EliteSharpLib.Views;

// Elite's flight controls are digital - each held key steps pitch or roll by
// a fixed amount - so a digital stick only has to say which way it is
// pushed, and sits at the ends of the range, so it passes the threshold
// whatever it is set to.
//
// An analog stick says more than that, and Deflection is what carries it:
// how far over the stick is, which the caller turns straight into a rate of
// turn rather than feeding it to the ramp. The two live side by side here
// because one device can be both at once - a SideWinder twists on a
// potentiometer but hats on switches.
//
// What each button does depends on the device, because the three sticks
// this supports disagree about every one of them - see GamepadProfile. The
// flight axes need no profile: a stick's X is its X on all three.
//
// Docking, hyperspace, ECM, the market and the charts stay on the keyboard.
// No stick here has buttons left for them.
internal static class GamepadControls
{
    private const float Threshold = 0.5f;

    // Enough to clear a worn potentiometer's wander. A SideWinder Precision 2
    // sitting untouched reports up to 0.03 on its X axis, drifting
    // continuously, so anything under this is the stick's rest position
    // rather than the pilot's hand.
    private const float Deadzone = 0.1f;

    // Negative rolls left, positive right, 0 is centred.
    internal static int Roll(IGamepad gamepad) => Direction(gamepad.Axis(GamepadAxis.LeftX));

    // Negative is the stick pushed forward, which does what S/Up does on the
    // keyboard; positive is pulled back, which does what X/Down does. SDL's Y
    // axis is positive downwards, the same sense the screen has.
    internal static int Pitch(IGamepad gamepad) => Direction(gamepad.Axis(GamepadAxis.LeftY));

    // A flight stick's twist. SDL gives a raw joystick nothing but axis
    // indices, and the twist is index 2, which the port already names
    // RightX - so a SideWinder Precision 2 yaws on Z Rotation without SDL
    // having to know the device. Negative yaws left, positive right.
    internal static int Yaw(IGamepad gamepad) => Direction(gamepad.Axis(GamepadAxis.RightX));

    // Which layout this device gets. Substring matches, because a driver is
    // free to decorate the name and SDL passes on whatever it is given.
    internal static GamepadProfile Profile(IGamepad gamepad)
    {
        ArgumentNullException.ThrowIfNull(gamepad);

        string name = gamepad.DeviceName;

        if (name.Contains("SideWinder", StringComparison.OrdinalIgnoreCase))
        {
            return GamepadProfile.SideWinder;
        }

        // The Competition Pro Extra reports its controller chip's part
        // number rather than anything a player would recognise.
        return name.Contains("STK-7024X", StringComparison.OrdinalIgnoreCase)
            ? GamepadProfile.CompetitionPro
            : GamepadProfile.Standard;
    }

    // A pad fires on its right trigger, not on (A): (A) slows the ship
    // down, and one control cannot do both. An unrecognised device gets
    // this layout, so a pad with no triggers cannot fire the laser - it can
    // still answer the "press fire" prompts below, which take any button.
    internal static bool IsFiring(IGamepad gamepad) => Profile(gamepad) switch
    {
        GamepadProfile.SideWinder => gamepad.IsHeld(GamepadButton.A),
        GamepadProfile.CompetitionPro => gamepad.IsHeld(GamepadButton.X),
        _ => IsPulled(gamepad, GamepadAxis.RightTrigger),
    };

    // Fire as a one-shot, for the "press fire to continue" prompts: IsFiring
    // is held-based, so it would fire again on every tick the button stays
    // down and skip straight through the screen behind it.
    //
    // Every button is taken here, not the profile's fire button only. These
    // prompts are the first thing a player meets, and a stick this does not
    // recognise should still get past the title screen.
    internal static bool WasFirePressed(IGamepad gamepad)
    {
        ArgumentNullException.ThrowIfNull(gamepad);

        return gamepad.IsPressed(GamepadButton.A)
            || gamepad.IsPressed(GamepadButton.B)
            || gamepad.IsPressed(GamepadButton.X)
            || gamepad.IsPressed(GamepadButton.Y);
    }

    internal static bool IsAccelerating(IGamepad gamepad) => Profile(gamepad) switch
    {
        // The SideWinder's throttle lever owns speed outright, so no button
        // does. Throttle() is what the caller reads instead.
        GamepadProfile.SideWinder => false,
        GamepadProfile.CompetitionPro => gamepad.IsHeld(GamepadButton.B),
        _ => gamepad.IsHeld(GamepadButton.B),
    };

    internal static bool IsDecelerating(IGamepad gamepad) => Profile(gamepad) switch
    {
        GamepadProfile.SideWinder => false,
        GamepadProfile.CompetitionPro => gamepad.IsHeld(GamepadButton.Y),
        _ => gamepad.IsHeld(GamepadButton.A),
    };

    // Missiles have to fire once per pull, not on every tick the control is
    // down. These report the control's state rather than an edge, and the
    // caller makes the edge - because one of the three fires on a trigger,
    // and a trigger is an axis with no press of its own to consume.
    internal static bool IsFireMissileHeld(IGamepad gamepad) => Profile(gamepad) switch
    {
        GamepadProfile.SideWinder => gamepad.IsHeld(GamepadButton.B),
        GamepadProfile.CompetitionPro => gamepad.IsHeld(GamepadButton.A),
        _ => IsPulled(gamepad, GamepadAxis.LeftTrigger),
    };

    // A Competition Pro has four buttons and five things worth doing, so it
    // is the one that loses out: targeting stays on the keyboard's T.
    internal static bool IsTargetMissileHeld(IGamepad gamepad) => Profile(gamepad) switch
    {
        GamepadProfile.SideWinder => gamepad.IsHeld(GamepadButton.Y),
        GamepadProfile.CompetitionPro => false,
        _ => gamepad.IsHeld(GamepadButton.LeftShoulder),
    };

    // Letting a target go again. Only the SideWinder has a button spare for
    // it - the eighth control on the one stick with eight - so everything
    // else keeps it on the keyboard's U.
    internal static bool IsUntargetMissileHeld(IGamepad gamepad) => Profile(gamepad) switch
    {
        GamepadProfile.SideWinder => gamepad.IsHeld(GamepadButton.X),
        _ => false,
    };

    // The four base buttons, which only the SideWinder has. They sit under
    // the other hand rather than the throttle hand, so what goes here is
    // what a pilot reaches for deliberately - and ECM, which is the one
    // reflex left over once the thumb cluster is full.
    //
    // The escape capsule is deliberately not among them: a base button
    // brushed by accident would end the run, and no convenience is worth
    // that. It stays on Esc.
    internal static bool IsEcmHeld(IGamepad gamepad) => Profile(gamepad) switch
    {
        GamepadProfile.SideWinder => gamepad.IsHeld(GamepadButton.LeftShoulder),
        _ => false,
    };

    internal static bool IsWarpJumpHeld(IGamepad gamepad) => Profile(gamepad) switch
    {
        GamepadProfile.SideWinder => gamepad.IsHeld(GamepadButton.RightShoulder),
        _ => false,
    };

    // One button for both halves of the docking computer, because the stick
    // has one to spare and the keyboard needs two (C engages, D disengages).
    // The caller decides which it means from whether the autopilot is on.
    internal static bool IsDockingComputerHeld(IGamepad gamepad) => Profile(gamepad) switch
    {
        GamepadProfile.SideWinder => gamepad.IsHeld(GamepadButton.Back),
        _ => false,
    };

    // Ordinary hyperspace only. Galactic hyperspace needs a modifier, and a
    // commander carries perhaps two in a game, so it is not worth a button.
    internal static bool IsHyperspaceHeld(IGamepad gamepad) => Profile(gamepad) switch
    {
        GamepadProfile.SideWinder => gamepad.IsHeld(GamepadButton.Start),
        _ => false,
    };

    // Where the throttle lever is set, 0 at the back through 1 fully
    // forward, or null when the device has no lever - which is every device
    // but the SideWinder, so speed stays on their buttons.
    //
    // Pushed forward is fast, the same sense as the stick and as a real
    // throttle. SDL reports forward as negative, hence the flip.
    internal static float? Throttle(IGamepad gamepad)
    {
        ArgumentNullException.ThrowIfNull(gamepad);

        return gamepad.IsAnalog(GamepadAxis.Throttle)
            ? (1 - gamepad.Axis(GamepadAxis.Throttle)) / 2
            : null;
    }

    // The hat, as a one-shot per direction: a view is selected once when the
    // hat goes over, not on every tick it is held there.
    internal static bool WasViewSelected(IGamepad gamepad, GamepadButton direction)
    {
        ArgumentNullException.ThrowIfNull(gamepad);

        return gamepad.IsPressed(direction);
    }

    // How far an analog axis is pushed, -1 to +1, with the deadzone taken out
    // and the rest stretched back over the full range - otherwise the travel
    // just outside the deadzone would start at a tenth rather than at nothing,
    // and a stick eased off centre would jump.
    //
    // Zero for an axis that is not analog (or has never moved), which is what
    // makes the caller fall through to the digital path: a device that cannot
    // report a position has no position to report.
    internal static float Deflection(IGamepad gamepad, GamepadAxis axis)
    {
        ArgumentNullException.ThrowIfNull(gamepad);

        if (!gamepad.IsAnalog(axis))
        {
            return 0;
        }

        float value = gamepad.Axis(axis);
        float magnitude = MathF.Abs(value);

        return magnitude <= Deadzone ? 0 : MathF.CopySign((magnitude - Deadzone) / (1 - Deadzone), value);
    }

    private static bool IsPulled(IGamepad gamepad, GamepadAxis trigger)
        => gamepad.Axis(trigger) >= Threshold;

    private static int Direction(float value) => value <= -Threshold ? -1 : value >= Threshold ? 1 : 0;
}

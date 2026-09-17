// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Abstraction.Controls;
using SharpKind.Input;

namespace EliteSharpLib.Controls;

/// <summary>
/// The bindings Elite ships with: what the code did before any of this was
/// a file. Everything here is a fact about Elite and about the three sticks
/// it was tested against, which is why none of it is in the library.
/// </summary>
internal static class EliteControlDefaults
{
    private const string SideWinderDevice = "Microsoft SideWinder Precision 2 Joystick";

    // The Competition Pro Extra reports its controller chip's part number, not its own name.
    private const string CompetitionProDevice = "STK-7024X";

    internal static ControlBindings Create()
    {
        ControlBindings bindings = new();

        foreach ((EliteAction action, KeyList keys) in Keyboard())
        {
            bindings.Keyboard[action.ToString()] = keys;
        }

        bindings.Controllers.Add(SideWinder());
        bindings.Controllers.Add(CompetitionPro());
        bindings.Controllers.Add(AnyPad());

        return bindings;
    }

    private static Dictionary<EliteAction, KeyList> Keyboard() => new()
    {
        [EliteAction.FireLaser] = [nameof(ConsoleKey.A)],
        [EliteAction.PitchUp] = [nameof(ConsoleKey.S), nameof(ConsoleKey.UpArrow)],
        [EliteAction.PitchDown] = [nameof(ConsoleKey.X), nameof(ConsoleKey.DownArrow)],
        [EliteAction.RollLeft] = [nameof(ConsoleKey.OemComma), nameof(ConsoleKey.LeftArrow)],
        [EliteAction.RollRight] = [nameof(ConsoleKey.OemPeriod), nameof(ConsoleKey.RightArrow)],
        [EliteAction.YawLeft] = [nameof(ConsoleKey.Q)],
        [EliteAction.YawRight] = [nameof(ConsoleKey.W)],
        [EliteAction.SpeedUp] = [nameof(ConsoleKey.Spacebar)],
        [EliteAction.SlowDown] = [nameof(ConsoleKey.Oem2)],
        [EliteAction.FireMissile] = [nameof(ConsoleKey.M)],
        [EliteAction.TargetMissile] = [nameof(ConsoleKey.T)],
        [EliteAction.UntargetMissile] = [nameof(ConsoleKey.U)],
        [EliteAction.Ecm] = [nameof(ConsoleKey.E)],
        [EliteAction.EnergyBomb] = [nameof(ConsoleKey.Tab)],
        [EliteAction.EscapeCapsule] = [nameof(ConsoleKey.Escape)],
        [EliteAction.WarpJump] = [nameof(ConsoleKey.J)],
        [EliteAction.Hyperspace] = [nameof(ConsoleKey.H)],
        [EliteAction.DockingComputerOn] = [nameof(ConsoleKey.C)],
        [EliteAction.DockingComputerOff] = [nameof(ConsoleKey.D)],
        [EliteAction.Pause] = [nameof(ConsoleKey.P)],
        [EliteAction.FrontView] = [nameof(ConsoleKey.F1)],
        [EliteAction.RearView] = [nameof(ConsoleKey.F2)],
        [EliteAction.LeftView] = [nameof(ConsoleKey.F3)],
        [EliteAction.RightView] = [nameof(ConsoleKey.F4)],
        [EliteAction.GalacticChart] = [nameof(ConsoleKey.F5)],
        [EliteAction.ShortRangeChart] = [nameof(ConsoleKey.F6)],
        [EliteAction.PlanetData] = [nameof(ConsoleKey.F7)],
        [EliteAction.MarketPrices] = [nameof(ConsoleKey.F8)],
        [EliteAction.CommanderStatus] = [nameof(ConsoleKey.F9)],
        [EliteAction.Inventory] = [nameof(ConsoleKey.F10)],
        [EliteAction.Options] = [nameof(ConsoleKey.F11)],
    };

    // Galactic hyperspace needs a modifier, so it's off-stick; so is the escape
    // capsule, since a button brushed by accident would end the run.
    private static ControllerBindings SideWinder()
    {
        ControllerBindings stick = new() { Device = SideWinderDevice };

        Axis(stick, EliteAxis.Roll, GamepadAxis.LeftX);
        Axis(stick, EliteAxis.Pitch, GamepadAxis.LeftY);
        Axis(stick, EliteAxis.Yaw, GamepadAxis.RightX);
        Axis(stick, EliteAxis.Speed, GamepadAxis.Throttle);

        Button(stick, EliteAction.FireLaser, GamepadButton.A);
        Button(stick, EliteAction.FireMissile, GamepadButton.B);
        Button(stick, EliteAction.UntargetMissile, GamepadButton.X);
        Button(stick, EliteAction.TargetMissile, GamepadButton.Y);
        Button(stick, EliteAction.Ecm, GamepadButton.LeftShoulder);
        Button(stick, EliteAction.WarpJump, GamepadButton.RightShoulder);
        Button(stick, EliteAction.DockingComputerToggle, GamepadButton.Back);
        Button(stick, EliteAction.Hyperspace, GamepadButton.Start);
        Button(stick, EliteAction.FrontView, GamepadButton.DPadUp);
        Button(stick, EliteAction.RearView, GamepadButton.DPadDown);
        Button(stick, EliteAction.LeftView, GamepadButton.DPadLeft);
        Button(stick, EliteAction.RightView, GamepadButton.DPadRight);

        return stick;
    }

    // Four buttons and nothing else, so two go on speed and missile targeting stays on the keyboard.
    private static ControllerBindings CompetitionPro()
    {
        ControllerBindings stick = new() { Device = CompetitionProDevice };

        Axis(stick, EliteAxis.Roll, GamepadAxis.LeftX);
        Axis(stick, EliteAxis.Pitch, GamepadAxis.LeftY);

        Button(stick, EliteAction.FireMissile, GamepadButton.A);
        Button(stick, EliteAction.SpeedUp, GamepadButton.B);
        Button(stick, EliteAction.FireLaser, GamepadButton.X);
        Button(stick, EliteAction.SlowDown, GamepadButton.Y);

        return stick;
    }

    // Both face buttons are spent on speed, so the lasers go on the triggers.
    private static ControllerBindings AnyPad()
    {
        ControllerBindings pad = new() { Device = ControlBindings.AnyDevice };

        Axis(pad, EliteAxis.Roll, GamepadAxis.LeftX);
        Axis(pad, EliteAxis.Pitch, GamepadAxis.LeftY);
        Axis(pad, EliteAxis.Yaw, GamepadAxis.RightX);

        Button(pad, EliteAction.SpeedUp, GamepadButton.B);
        Button(pad, EliteAction.SlowDown, GamepadButton.A);
        Button(pad, EliteAction.TargetMissile, GamepadButton.LeftShoulder);
        Button(pad, EliteAction.FrontView, GamepadButton.DPadUp);
        Button(pad, EliteAction.RearView, GamepadButton.DPadDown);
        Button(pad, EliteAction.LeftView, GamepadButton.DPadLeft);
        Button(pad, EliteAction.RightView, GamepadButton.DPadRight);

        pad.Triggers[nameof(EliteAction.FireLaser)] = nameof(GamepadAxis.RightTrigger);
        pad.Triggers[nameof(EliteAction.FireMissile)] = nameof(GamepadAxis.LeftTrigger);

        return pad;
    }

    private static void Axis(ControllerBindings controller, EliteAxis axis, GamepadAxis device)
        => controller.Axes[axis.ToString()] = new() { Axis = device.ToString() };

    private static void Button(ControllerBindings controller, EliteAction action, GamepadButton button)
        => controller.Buttons[action.ToString()] = button.ToString();
}

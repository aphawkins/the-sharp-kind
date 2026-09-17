// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Controls;
using SharpKind.Abstraction.Controls;
using SharpKind.Fakes.Input;
using SharpKind.Input;

namespace EliteSharpLib.Tests.Controls;

// Same checks the hardcoded layouts had, now asked of the file.
public class ControlMapTests
{
    private const string SideWinder = "Microsoft SideWinder Precision 2 Joystick";
    private const string CompetitionPro = "STK-7024X";
    private const string Xbox = "Xbox One Controller";

    // Button 1 means something different per device; no single assignment could serve all three.
    [Theory]
    [InlineData(SideWinder, EliteAction.FireLaser)]
    [InlineData(CompetitionPro, EliteAction.FireMissile)]
    [InlineData(Xbox, EliteAction.SlowDown)]
    internal void ButtonOneMeansSomethingDifferentOnEachStick(string device, EliteAction expected)
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(device);
        pad.ButtonDown(GamepadButton.A);

        Assert.True(controls.IsHeld(expected));
    }

    [Theory]
    [InlineData(SideWinder, GamepadButton.A)]
    [InlineData(CompetitionPro, GamepadButton.X)]
    internal void EachStickFiresOnItsOwnButton(string device, GamepadButton button)
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(device);
        pad.ButtonDown(button);

        Assert.True(controls.IsHeld(EliteAction.FireLaser));
    }

    // Both face buttons are spent on speed, so a pad fires on trigger axis threshold, not a press.
    [Fact]
    internal void APadFiresOnItsTriggers()
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(Xbox);
        pad.AxisMoved(GamepadAxis.RightTrigger, 1f);

        Assert.True(controls.IsHeld(EliteAction.FireLaser));

        pad.AxisMoved(GamepadAxis.LeftTrigger, 1f);

        Assert.True(controls.IsHeld(EliteAction.FireMissile));
    }

    // The SideWinder's lever owns speed outright; no button may also claim it.
    [Fact]
    internal void TheSideWinderKeepsNoSpeedButtons()
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(SideWinder);

        foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
        {
            pad.ButtonDown(button);

            Assert.False(controls.IsHeld(EliteAction.SpeedUp));
            Assert.False(controls.IsHeld(EliteAction.SlowDown));
        }
    }

    // Each of the SideWinder's eight buttons does one thing, or two commands run on a single press.
    [Theory]
    [InlineData(GamepadButton.A)]
    [InlineData(GamepadButton.B)]
    [InlineData(GamepadButton.X)]
    [InlineData(GamepadButton.Y)]
    [InlineData(GamepadButton.LeftShoulder)]
    [InlineData(GamepadButton.RightShoulder)]
    [InlineData(GamepadButton.Back)]
    [InlineData(GamepadButton.Start)]
    internal void EachSideWinderButtonDoesOneThing(GamepadButton button)
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(SideWinder);
        pad.ButtonDown(button);

        int claimed = Enum.GetValues<EliteAction>()
            .Count(action => action != EliteAction.None && controls.IsHeld(action));

        Assert.Equal(1, claimed);
    }

    [Theory]
    [InlineData(GamepadButton.DPadUp, EliteAction.FrontView)]
    [InlineData(GamepadButton.DPadDown, EliteAction.RearView)]
    [InlineData(GamepadButton.DPadLeft, EliteAction.LeftView)]
    [InlineData(GamepadButton.DPadRight, EliteAction.RightView)]
    internal void TheHatSelectsTheViews(GamepadButton direction, EliteAction expected)
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(SideWinder);
        pad.ButtonDown(direction);

        Assert.True(controls.WasPressed(expected));
    }

    // SDL reports forward as negative; the flip to "forward is fast" is the game's convention, not the file's.
    [Theory]
    [InlineData(-1f, 1f)]
    [InlineData(0f, 0.5f)]
    [InlineData(1f, 0f)]
    internal void TheThrottleReadsForwardAsFast(float axis, float expected)
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(SideWinder);
        pad.AxisMoved(GamepadAxis.Throttle, 0.3f);
        pad.AxisMoved(GamepadAxis.Throttle, axis);

        Assert.Equal(expected, controls.Throttle(EliteAxis.Speed)!.Value, 3);
    }

    // A device with no lever must report none, or a centred axis reads as half speed.
    [Theory]
    [InlineData(Xbox)]
    [InlineData(CompetitionPro)]
    internal void ADeviceWithNoLeverHasNoThrottle(string device)
    {
        (EliteControlMap controls, _, _) = Map(device);

        Assert.Null(controls.Throttle(EliteAxis.Speed));
    }

    // Deflection is the stick's raw position; the roll-right-is-negative convention lives in the game, not here.
    [Fact]
    internal void TheDeflectionIsTheStickPosition()
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(SideWinder);
        pad.AxisMoved(GamepadAxis.LeftX, 0.3f);
        pad.AxisMoved(GamepadAxis.LeftX, 1f);

        Assert.Equal(1f, controls.Deflection(EliteAxis.Roll), 3);
    }

    // A digital stick has no position, so its axis answers the direction actions instead.
    [Theory]
    [InlineData(GamepadAxis.LeftX, -1f, EliteAction.RollLeft)]
    [InlineData(GamepadAxis.LeftX, 1f, EliteAction.RollRight)]
    [InlineData(GamepadAxis.LeftY, -1f, EliteAction.PitchUp)]
    [InlineData(GamepadAxis.LeftY, 1f, EliteAction.PitchDown)]
    internal void ADigitalStickHoldsADirection(GamepadAxis axis, float value, EliteAction expected)
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(CompetitionPro);
        pad.AxisMoved(axis, value);

        Assert.True(controls.IsHeld(expected));
    }

    // A stick resting at centre holds no direction, or the ship would turn on its own.
    [Fact]
    internal void ACentredStickHoldsNoDirection()
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(CompetitionPro);
        pad.AxisMoved(GamepadAxis.LeftX, 0f);
        pad.AxisMoved(GamepadAxis.LeftY, 0f);

        Assert.False(controls.IsHeld(EliteAction.RollLeft));
        Assert.False(controls.IsHeld(EliteAction.RollRight));
        Assert.False(controls.IsHeld(EliteAction.PitchUp));
        Assert.False(controls.IsHeld(EliteAction.PitchDown));
    }

    // A digital stick has no position to report, so the caller falls through to the ramp.
    [Fact]
    internal void ADigitalStickHasNoDeflection()
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(CompetitionPro);
        pad.AxisMoved(GamepadAxis.LeftX, 1f);

        Assert.False(controls.IsAnalog(EliteAxis.Roll));
        Assert.Equal(0f, controls.Deflection(EliteAxis.Roll));
    }

    // A worn potentiometer wanders around its centre, and none of that is the pilot.
    [Fact]
    internal void TheDeadzoneSwallowsAStickAtRest()
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map(SideWinder);
        pad.AxisMoved(GamepadAxis.LeftX, 0.3f);
        pad.AxisMoved(GamepadAxis.LeftX, 0.03f);

        Assert.Equal(0f, controls.Deflection(EliteAxis.Roll));
    }

    [Fact]
    internal void TheKeyboardAnswersForEveryDevice()
    {
        (EliteControlMap controls, FakeKeyboard keyboard, _) = Map(SideWinder);
        keyboard.KeyDown(ConsoleKey.A, default);

        Assert.True(controls.IsHeld(EliteAction.FireLaser));
    }

    // An unrecognised stick gets the catch-all entry rather than nothing, so it can still be flown.
    [Fact]
    internal void AnUnknownDeviceGetsTheCatchAllEntry()
    {
        (EliteControlMap controls, _, FakeGamepad pad) = Map("Some Unknown Stick");
        pad.ButtonDown(GamepadButton.B);

        Assert.True(controls.IsHeld(EliteAction.SpeedUp));
    }

    // Nothing attached must not throw, and must not claim a control.
    [Fact]
    internal void NoDeviceMeansNoControllerBindings()
    {
        ControlBindings bindings = EliteControlDefaults.Create();
        FakeKeyboard keyboard = new();
        FakeGamepad pad = new();
        EliteControlMap controls = new(bindings, keyboard, pad);

        Assert.False(controls.IsHeld(EliteAction.FireLaser));
        Assert.Null(controls.Throttle(EliteAxis.Speed));
        Assert.Equal(0f, controls.Deflection(EliteAxis.Roll));
    }

    // Two sticks attached and only the chosen one flies the ship.
    [Fact]
    internal void OnlyTheActiveDeviceIsBound()
    {
        ControlBindings bindings = EliteControlDefaults.Create();
        FakeKeyboard keyboard = new();
        FakeGamepad pad = new();
        pad.Connected(1, Xbox);
        pad.Connected(2, SideWinder);
        EliteControlMap controls = new(bindings, keyboard, pad);

        // (A) is slow-down on the pad, which is the first attached.
        pad.ButtonDown(1, GamepadButton.A);
        Assert.True(controls.IsHeld(EliteAction.SlowDown));

        pad.PreferredDevice = SideWinder;
        pad.ButtonDown(2, GamepadButton.A);

        // Same button fires the laser on the stick; the pad's slow-down is no longer anybody's business.
        Assert.True(controls.IsHeld(EliteAction.FireLaser));
        Assert.False(controls.IsHeld(EliteAction.SlowDown));
    }

    private static (EliteControlMap Controls, FakeKeyboard Keyboard, FakeGamepad Pad) Map(string device)
    {
        FakeKeyboard keyboard = new();
        FakeGamepad pad = new();
        pad.Connected(device);

        return (new EliteControlMap(EliteControlDefaults.Create(), keyboard, pad), keyboard, pad);
    }
}

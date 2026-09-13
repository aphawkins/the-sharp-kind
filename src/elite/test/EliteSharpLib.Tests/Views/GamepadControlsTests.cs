// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Views;
using SharpKind.Fakes.Input;
using SharpKind.Input;

namespace EliteSharpLib.Tests.Views;

public class GamepadControlsTests
{
    private const string SideWinder = "Microsoft SideWinder Precision 2 Joystick";
    private const string CompetitionPro = "STK-7024X";
    private const string Xbox = "Xbox One Controller";

    [Fact]
    public void AnIdlePadAsksForNothing()
    {
        FakeGamepad pad = new();

        Assert.Equal(0, GamepadControls.Roll(pad));
        Assert.Equal(0, GamepadControls.Pitch(pad));
        Assert.Equal(0, GamepadControls.Yaw(pad));
        Assert.False(GamepadControls.IsFiring(pad));
        Assert.False(GamepadControls.IsAccelerating(pad));
        Assert.False(GamepadControls.IsDecelerating(pad));
    }

    // A stick has to travel a good way before it counts, so a resting
    // stick's drift cannot roll the ship. A digital stick reports the ends of
    // the range, so it always passes.
    [Theory]
    [InlineData(-1f, -1)]
    [InlineData(-0.6f, -1)]
    [InlineData(-0.4f, 0)]
    [InlineData(0f, 0)]
    [InlineData(0.4f, 0)]
    [InlineData(0.6f, 1)]
    [InlineData(1f, 1)]
    public void TheStickRollsOnlyPastTheThreshold(float x, int expected)
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.LeftX, x);

        Assert.Equal(expected, GamepadControls.Roll(pad));
    }

    // Pulling back climbs: SDL reports that as negative, the same sense
    // Elite's own "up" control has.
    [Theory]
    [InlineData(-1f, -1)]
    [InlineData(0f, 0)]
    [InlineData(1f, 1)]
    public void TheStickPitches(float y, int expected)
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.LeftY, y);

        Assert.Equal(expected, GamepadControls.Pitch(pad));
    }

    // The twist axis. SDL numbers a raw joystick's axes and says nothing
    // about what they are, so a SideWinder's Z Rotation arrives as index 2,
    // which the port names RightX.
    [Theory]
    [InlineData(-1f, -1)]
    [InlineData(0f, 0)]
    [InlineData(1f, 1)]
    public void TheStickYawsOnItsTwist(float twist, int expected)
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.RightX, twist);

        Assert.Equal(expected, GamepadControls.Yaw(pad));
    }

    // The three sticks disagree about every button, so each is checked
    // against its own layout. These are the mappings the devices were
    // observed to report, not a guess: see docs/elite-readme.md.
    [Theory]
    [InlineData(SideWinder, GamepadButton.A)]
    [InlineData(CompetitionPro, GamepadButton.X)]
    public void EachStickFiresOnItsOwnButton(string device, GamepadButton button)
    {
        FakeGamepad pad = Device(device);
        pad.ButtonDown(button);

        Assert.True(GamepadControls.IsFiring(pad));
    }

    // The clearest case for profiles at all: button 1 fires the laser on a
    // SideWinder, fires a missile on a Competition Pro, and slows an Xbox
    // pad down. No single assignment could serve all three.
    [Fact]
    public void ButtonOneMeansSomethingDifferentOnEachStick()
    {
        FakeGamepad sidewinder = Device(SideWinder);
        sidewinder.ButtonDown(GamepadButton.A);
        Assert.True(GamepadControls.IsFiring(sidewinder));

        FakeGamepad competitionPro = Device(CompetitionPro);
        competitionPro.ButtonDown(GamepadButton.A);
        Assert.True(GamepadControls.IsFireMissileHeld(competitionPro));
        Assert.False(GamepadControls.IsFiring(competitionPro));

        FakeGamepad xbox = Device(Xbox);
        xbox.ButtonDown(GamepadButton.A);
        Assert.True(GamepadControls.IsDecelerating(xbox));
        Assert.False(GamepadControls.IsFiring(xbox));
    }

    // Firing is polled every frame, so a held button must survive repeated
    // reads - IsPressed's one-shot consumption would not.
    [Fact]
    public void AHeldFireButtonKeepsFiring()
    {
        FakeGamepad pad = Device(SideWinder);
        pad.ButtonDown(GamepadButton.A);

        Assert.True(GamepadControls.IsFiring(pad));
        Assert.True(GamepadControls.IsFiring(pad));
        Assert.True(GamepadControls.IsFiring(pad));
    }

    // A pad fires on its triggers, because both of its face buttons are
    // spent on speed. It has no throttle lever to take that over.
    [Fact]
    public void APadFiresOnItsTriggers()
    {
        FakeGamepad pad = Device(Xbox);
        pad.AxisMoved(GamepadAxis.RightTrigger, 1f);
        Assert.True(GamepadControls.IsFiring(pad));

        pad.AxisMoved(GamepadAxis.LeftTrigger, 1f);
        Assert.True(GamepadControls.IsFireMissileHeld(pad));

        pad.ButtonDown(GamepadButton.LeftShoulder);
        Assert.True(GamepadControls.IsTargetMissileHeld(pad));
    }

    [Fact]
    public void APadKeepsSpeedOnItsFaceButtons()
    {
        FakeGamepad pad = Device(Xbox);
        pad.ButtonDown(GamepadButton.B);
        Assert.True(GamepadControls.IsAccelerating(pad));

        pad.ButtonUp(GamepadButton.B);
        pad.ButtonDown(GamepadButton.A);
        Assert.True(GamepadControls.IsDecelerating(pad));
    }

    // The Competition Pro spends all four of its buttons, two of them on
    // speed, because it has no lever and no triggers.
    [Fact]
    public void TheCompetitionProPutsSpeedOnTwoOfItsFourButtons()
    {
        FakeGamepad pad = Device(CompetitionPro);
        pad.ButtonDown(GamepadButton.B);
        Assert.True(GamepadControls.IsAccelerating(pad));

        pad.ButtonUp(GamepadButton.B);
        pad.ButtonDown(GamepadButton.Y);
        Assert.True(GamepadControls.IsDecelerating(pad));
    }

    // The SideWinder's lever owns speed outright, so no button on it may
    // also claim speed - or the lever and the button would fight.
    [Fact]
    public void TheSideWinderKeepsNoSpeedButtons()
    {
        FakeGamepad pad = Device(SideWinder);

        foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
        {
            pad.ButtonDown(button);

            Assert.False(GamepadControls.IsAccelerating(pad));
            Assert.False(GamepadControls.IsDecelerating(pad));
        }
    }

    // No stick may fire its laser and move its throttle with one button.
    [Theory]
    [InlineData(SideWinder)]
    [InlineData(CompetitionPro)]
    [InlineData(Xbox)]
    public void FiringNeverAlsoChangesSpeed(string device)
    {
        foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
        {
            FakeGamepad pad = Device(device);
            pad.ButtonDown(button);

            if (!GamepadControls.IsFiring(pad))
            {
                continue;
            }

            Assert.False(GamepadControls.IsAccelerating(pad));
            Assert.False(GamepadControls.IsDecelerating(pad));
        }
    }

    // The SideWinder is the only stick with a button to spare for letting a
    // target go, so it is the only one that has it.
    [Fact]
    public void OnlyTheSideWinderUntargetsAMissile()
    {
        FakeGamepad sidewinder = Device(SideWinder);
        sidewinder.ButtonDown(GamepadButton.X);
        Assert.True(GamepadControls.IsUntargetMissileHeld(sidewinder));

        foreach (string device in new[] { CompetitionPro, Xbox })
        {
            foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
            {
                FakeGamepad pad = Device(device);
                pad.ButtonDown(button);

                Assert.False(GamepadControls.IsUntargetMissileHeld(pad));
            }
        }
    }

    // All eight of the SideWinder's buttons are spoken for, and each does
    // one thing only - or two commands would run on a single press.
    [Theory]
    [InlineData(GamepadButton.A)]
    [InlineData(GamepadButton.B)]
    [InlineData(GamepadButton.X)]
    [InlineData(GamepadButton.Y)]
    [InlineData(GamepadButton.LeftShoulder)]
    [InlineData(GamepadButton.RightShoulder)]
    [InlineData(GamepadButton.Back)]
    [InlineData(GamepadButton.Start)]
    public void EachSideWinderButtonDoesOneThing(GamepadButton button)
    {
        FakeGamepad pad = Device(SideWinder);
        pad.ButtonDown(button);

        Assert.Equal(1, Claimed(pad));
    }

    // The base buttons are the SideWinder's alone; no other device grows
    // controls it has no buttons for.
    [Theory]
    [InlineData(CompetitionPro)]
    [InlineData(Xbox)]
    public void OnlyTheSideWinderHasTheBaseCommands(string device)
    {
        foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
        {
            FakeGamepad pad = Device(device);
            pad.ButtonDown(button);

            Assert.False(GamepadControls.IsEcmHeld(pad));
            Assert.False(GamepadControls.IsWarpJumpHeld(pad));
            Assert.False(GamepadControls.IsDockingComputerHeld(pad));
            Assert.False(GamepadControls.IsHyperspaceHeld(pad));
        }
    }

    // The base buttons, in the order they are wired.
    [Theory]
    [InlineData(GamepadButton.LeftShoulder, "ecm")]
    [InlineData(GamepadButton.RightShoulder, "jump")]
    [InlineData(GamepadButton.Back, "dock")]
    [InlineData(GamepadButton.Start, "hyperspace")]
    public void TheBaseButtonsTakeTheDeliberateCommands(GamepadButton button, string expected)
    {
        FakeGamepad pad = Device(SideWinder);
        pad.ButtonDown(button);

        string actual = GamepadControls.IsEcmHeld(pad) ? "ecm"
            : GamepadControls.IsWarpJumpHeld(pad) ? "jump"
            : GamepadControls.IsDockingComputerHeld(pad) ? "dock"
            : GamepadControls.IsHyperspaceHeld(pad) ? "hyperspace"
            : "none";

        Assert.Equal(expected, actual);
    }

    // The throttle reads 0 at the back and 1 fully forward. SDL reports
    // forward as negative, the same sense the stick has.
    [Theory]
    [InlineData(-1f, 1f)]
    [InlineData(0f, 0.5f)]
    [InlineData(1f, 0f)]
    public void TheThrottleReadsForwardAsFast(float axis, float expected)
    {
        FakeGamepad pad = Device(SideWinder);
        pad.AxisMoved(GamepadAxis.Throttle, 0.3f);
        pad.AxisMoved(GamepadAxis.Throttle, axis);

        Assert.Equal(expected, GamepadControls.Throttle(pad));
    }

    // A device with no lever must report none, or a centred axis would read
    // as half speed and hold the ship there.
    [Fact]
    public void ADeviceWithNoLeverHasNoThrottle()
    {
        Assert.Null(GamepadControls.Throttle(Device(Xbox)));
        Assert.Null(GamepadControls.Throttle(Device(CompetitionPro)));
    }

    // An unrecognised stick still has to get past the title screen, so the
    // "press fire" prompt takes any button rather than a profile's own.
    [Theory]
    [InlineData(GamepadButton.A)]
    [InlineData(GamepadButton.B)]
    [InlineData(GamepadButton.X)]
    [InlineData(GamepadButton.Y)]
    public void AnyButtonAnswersThePressFirePrompt(GamepadButton button)
    {
        FakeGamepad pad = new();
        pad.ButtonDown(button);

        Assert.True(GamepadControls.WasFirePressed(pad));
    }

    // A digital stick has no position to report, only an end of the range,
    // so it yields nothing here and keeps the ramp instead.
    [Theory]
    [InlineData(-1f)]
    [InlineData(0f)]
    [InlineData(1f)]
    public void ADigitalStickHasNoDeflection(float x)
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.LeftX, x);

        Assert.Equal(0, GamepadControls.Deflection(pad, GamepadAxis.LeftX));
    }

    // Once an axis has proved itself analog, its position is reported in
    // full - including the ends, which on their own would have looked digital.
    [Theory]
    [InlineData(0.5f, 0.4444444f)]
    [InlineData(-0.5f, -0.4444444f)]
    [InlineData(1f, 1f)]
    [InlineData(-1f, -1f)]
    public void AnAnalogStickReportsItsPosition(float x, float expected)
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.LeftX, 0.3f);
        pad.AxisMoved(GamepadAxis.LeftX, x);

        Assert.Equal(expected, GamepadControls.Deflection(pad, GamepadAxis.LeftX), 5);
    }

    // A worn potentiometer wanders around its centre - a SideWinder
    // Precision 2 reaches 0.03 untouched - and none of that is the pilot.
    [Theory]
    [InlineData(0.03f)]
    [InlineData(-0.03f)]
    [InlineData(0.1f)]
    [InlineData(-0.1f)]
    public void TheDeadzoneSwallowsAStickAtRest(float x)
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.LeftX, 0.3f);
        pad.AxisMoved(GamepadAxis.LeftX, x);

        Assert.Equal(0, GamepadControls.Deflection(pad, GamepadAxis.LeftX));
    }

    // Just outside the deadzone the stick starts from nothing rather than
    // from a tenth, so easing off centre eases the turn on.
    [Fact]
    public void TheDeflectionResumesFromZeroOutsideTheDeadzone()
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.LeftX, 0.1001f);

        Assert.Equal(0, GamepadControls.Deflection(pad, GamepadAxis.LeftX), 3);
    }

    [Theory]
    [InlineData("Microsoft SideWinder Precision 2 Joystick", GamepadProfile.SideWinder)]
    [InlineData("STK-7024X", GamepadProfile.CompetitionPro)]
    [InlineData("Xbox One Controller", GamepadProfile.Standard)]
    [InlineData("", GamepadProfile.Standard)]
    [InlineData("Some Unknown Stick", GamepadProfile.Standard)]
    internal void TheDeviceNamePicksTheProfile(string name, GamepadProfile expected)
    {
        FakeGamepad pad = new();
        pad.Connected(name);

        Assert.Equal(expected, GamepadControls.Profile(pad));
    }

    // How many commands a single button press claims. Every mapped button
    // must claim exactly one.
    private static int Claimed(FakeGamepad pad)
        => (GamepadControls.IsFiring(pad) ? 1 : 0)
            + (GamepadControls.IsFireMissileHeld(pad) ? 1 : 0)
            + (GamepadControls.IsTargetMissileHeld(pad) ? 1 : 0)
            + (GamepadControls.IsUntargetMissileHeld(pad) ? 1 : 0)
            + (GamepadControls.IsEcmHeld(pad) ? 1 : 0)
            + (GamepadControls.IsWarpJumpHeld(pad) ? 1 : 0)
            + (GamepadControls.IsDockingComputerHeld(pad) ? 1 : 0)
            + (GamepadControls.IsHyperspaceHeld(pad) ? 1 : 0)
            + (GamepadControls.IsAccelerating(pad) ? 1 : 0)
            + (GamepadControls.IsDecelerating(pad) ? 1 : 0);

    private static FakeGamepad Device(string name)
    {
        FakeGamepad pad = new();
        pad.Connected(name);

        return pad;
    }
}

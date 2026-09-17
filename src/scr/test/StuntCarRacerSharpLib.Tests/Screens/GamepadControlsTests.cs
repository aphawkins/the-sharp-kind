// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.Fakes.Input;
using SharpKind.Input;
using StuntCarRacerSharpLib.Cars;
using StuntCarRacerSharpLib.Screens;
using Xunit;

namespace StuntCarRacerSharpLib.Tests.Screens;

public class GamepadControlsTests
{
    [Fact]
    public void AnIdlePadAsksForNothing()
        => Assert.Equal(CarInput.None, GamepadControls.ReadCarInput(new FakeGamepad()));

    // An analog stick has to travel a good way before it counts as a steer, so resting drift can't creep the car sideways.
    [Theory]
    [InlineData(-1f, CarInput.Left)]
    [InlineData(-0.6f, CarInput.Left)]
    [InlineData(-0.4f, CarInput.None)]
    [InlineData(0f, CarInput.None)]
    [InlineData(0.4f, CarInput.None)]
    [InlineData(0.6f, CarInput.Right)]
    [InlineData(1f, CarInput.Right)]
    public void TheLeftStickSteersOnlyPastTheThreshold(float x, CarInput expected)
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.LeftX, x);

        Assert.Equal(expected, GamepadControls.ReadCarInput(pad));
    }

    // A one-stick joystick has no triggers, so forward and back on the stick
    // are accelerate and brake instead.
    [Theory]
    [InlineData(-1f, CarInput.Accelerate)]
    [InlineData(-0.6f, CarInput.Accelerate)]
    [InlineData(-0.4f, CarInput.None)]
    [InlineData(0.4f, CarInput.None)]
    [InlineData(0.6f, CarInput.Brake)]
    [InlineData(1f, CarInput.Brake)]
    public void StickForwardAcceleratesAndBackBrakes(float y, CarInput expected)
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.LeftY, y);

        Assert.Equal(expected, GamepadControls.ReadCarInput(pad));
    }

    // Buttons 1 and 3 as the device numbers them.
    [Theory]
    [InlineData(GamepadButton.A)]
    [InlineData(GamepadButton.X)]
    public void EitherFireButtonBoosts(GamepadButton button)
    {
        FakeGamepad pad = new();
        pad.ButtonDown(button);

        Assert.Equal(CarInput.Boost, GamepadControls.ReadCarInput(pad));
    }

    // The stick does both jobs at once on a joystick: forward and left is
    // accelerating into a left-hander, not one or the other.
    [Fact]
    public void TheStickSteersAndAcceleratesTogether()
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.LeftX, -1f);
        pad.AxisMoved(GamepadAxis.LeftY, -1f);

        Assert.Equal(CarInput.Left | CarInput.Accelerate, GamepadControls.ReadCarInput(pad));
    }

    [Fact]
    public void StickBackBeatsTheTriggerJustAsBrakeBeatsAccelerate()
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.RightTrigger, 1f);
        pad.AxisMoved(GamepadAxis.LeftY, 1f);

        Assert.Equal(CarInput.Brake, GamepadControls.ReadCarInput(pad));
    }

    [Fact]
    public void TheRightTriggerAccelerates()
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.RightTrigger, 1f);

        Assert.Equal(CarInput.Accelerate, GamepadControls.ReadCarInput(pad));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BrakeComesFromEitherBOrTheLeftTrigger(bool useTrigger)
    {
        FakeGamepad pad = new();
        if (useTrigger)
        {
            pad.AxisMoved(GamepadAxis.LeftTrigger, 1f);
        }
        else
        {
            pad.ButtonDown(GamepadButton.B);
        }

        Assert.Equal(CarInput.Brake, GamepadControls.ReadCarInput(pad));
    }

    // The remake clears accelerate when brake is selected
    // (Car_Behaviour.cpp:808-812), so both triggers at once brakes.
    [Fact]
    public void BrakeWinsOverAccelerate()
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.RightTrigger, 1f);
        pad.AxisMoved(GamepadAxis.LeftTrigger, 1f);

        Assert.Equal(CarInput.Brake, GamepadControls.ReadCarInput(pad));
    }

    [Fact]
    public void BoostAppliesAlongsideBrake()
    {
        FakeGamepad pad = new();
        pad.ButtonDown(GamepadButton.A);
        pad.ButtonDown(GamepadButton.B);

        Assert.Equal(CarInput.Boost | CarInput.Brake, GamepadControls.ReadCarInput(pad));
    }

    // Driving is polled every physics tick, so a held control must survive
    // repeated reads - IsPressed's one-shot consumption would not.
    [Fact]
    public void HeldControlsSurviveRepeatedReads()
    {
        FakeGamepad pad = new();
        pad.AxisMoved(GamepadAxis.RightTrigger, 1f);
        pad.ButtonDown(GamepadButton.A);

        const CarInput expected = CarInput.Accelerate | CarInput.Boost;

        Assert.Equal(expected, GamepadControls.ReadCarInput(pad));
        Assert.Equal(expected, GamepadControls.ReadCarInput(pad));
        Assert.Equal(expected, GamepadControls.ReadCarInput(pad));
    }
}

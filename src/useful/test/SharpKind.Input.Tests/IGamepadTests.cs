// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Fakes.Input;

namespace SharpKind.Input.Tests;

public class IGamepadTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialStateIsDisconnectedNothingPressedAxesZero(bool software)
    {
        IGamepad pad = Create(software);

        Assert.False(pad.IsConnected);
        Assert.False(pad.IsPressed(GamepadButton.A));
        Assert.False(pad.IsHeld(GamepadButton.A));
        Assert.Equal(0f, pad.Axis(GamepadAxis.LeftX));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ButtonDownIsPressedOnceButHeldUntilRelease(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;

        sink.ButtonDown(GamepadButton.A);

        Assert.True(pad.IsPressed(GamepadButton.A));
        Assert.False(pad.IsPressed(GamepadButton.A)); // one-shot: consumed
        Assert.True(pad.IsHeld(GamepadButton.A));
        Assert.True(pad.IsHeld(GamepadButton.A)); // continuous: not consumed

        sink.ButtonUp(GamepadButton.A);

        Assert.False(pad.IsHeld(GamepadButton.A));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoneButtonIsNeverPressedOrHeld(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;

        sink.ButtonDown(GamepadButton.None);

        Assert.False(pad.IsPressed(GamepadButton.None));
        Assert.False(pad.IsHeld(GamepadButton.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnalogAxisKeepsItsTravelAndIsNotConsumed(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;

        sink.AxisMoved(GamepadAxis.LeftX, -0.42f);

        Assert.Equal(-0.42f, pad.Axis(GamepadAxis.LeftX), 3);
        Assert.Equal(-0.42f, pad.Axis(GamepadAxis.LeftX), 3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DigitalDeviceReportsAxesAsMinusOneZeroPlusOne(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;

        sink.AxisMoved(GamepadAxis.LeftX, -1f);
        Assert.Equal(-1f, pad.Axis(GamepadAxis.LeftX));

        sink.AxisMoved(GamepadAxis.LeftX, 0f);
        Assert.Equal(0f, pad.Axis(GamepadAxis.LeftX));

        sink.AxisMoved(GamepadAxis.LeftX, 1f);
        Assert.Equal(1f, pad.Axis(GamepadAxis.LeftX));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AxisIsClampedToTheDocumentedRange(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;

        sink.AxisMoved(GamepadAxis.LeftX, -3f);
        Assert.Equal(-1f, pad.Axis(GamepadAxis.LeftX));

        sink.AxisMoved(GamepadAxis.LeftX, 3f);
        Assert.Equal(1f, pad.Axis(GamepadAxis.LeftX));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConnectedTracksAttachedDevicesAcrossHotplug(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;

        sink.Connected();
        Assert.True(pad.IsConnected);

        sink.Connected();
        sink.Disconnected();
        Assert.True(pad.IsConnected); // a second device is still attached

        sink.Disconnected();
        Assert.False(pad.IsConnected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnplugClearsHeldStateSoNothingStaysStuck(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;

        sink.Connected();
        sink.ButtonDown(GamepadButton.A);
        sink.AxisMoved(GamepadAxis.LeftX, -1f);

        sink.Disconnected();

        Assert.False(pad.IsHeld(GamepadButton.A));
        Assert.False(pad.IsPressed(GamepadButton.A));
        Assert.Equal(0f, pad.Axis(GamepadAxis.LeftX));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClearPressedRemovesEverything(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;

        sink.ButtonDown(GamepadButton.A);
        sink.ButtonDown(GamepadButton.B);
        sink.AxisMoved(GamepadAxis.RightTrigger, 1f);

        pad.ClearPressed();

        Assert.False(pad.IsPressed(GamepadButton.A));
        Assert.False(pad.IsHeld(GamepadButton.B));
        Assert.Equal(0f, pad.Axis(GamepadAxis.RightTrigger));
    }

    [Fact]
    public void SoftwareGamepadRegistersItselfWithTheInputBackend()
    {
        RecordingInput input = new();

        SoftwareGamepad pad = new(input);

        Assert.Same(pad, input.Gamepad);

        pad.Poll();

        Assert.Equal(1, input.PollCount);
    }

    [Fact]
    public void SoftwareGamepadRejectsANullInput()
        => Assert.Throws<ArgumentNullException>(() => new SoftwareGamepad(null!));

    // The whole basis of telling the two stick kinds apart: only a
    // potentiometer can report a position that is not an end or the centre.
    [Theory]
    [InlineData(-1f)]
    [InlineData(0f)]
    [InlineData(1f)]
    public void AnAxisReportingOnlyItsExtremesIsDigital(float value)
    {
        SoftwareGamepad pad = new(new FakeInput());
        pad.AxisMoved(GamepadAxis.LeftX, value);

        Assert.False(pad.IsAnalog(GamepadAxis.LeftX));
    }

    [Fact]
    public void AnAxisReportingAPositionBetweenIsAnalog()
    {
        SoftwareGamepad pad = new(new FakeInput());
        pad.AxisMoved(GamepadAxis.LeftX, 0.3f);

        Assert.True(pad.IsAnalog(GamepadAxis.LeftX));
    }

    // A run of extremes is only the stick being at an end, so it cannot
    // withdraw a conclusion the intermediate reading already proved.
    [Fact]
    public void AnAnalogAxisStaysAnalogAtItsExtremes()
    {
        SoftwareGamepad pad = new(new FakeInput());
        pad.AxisMoved(GamepadAxis.LeftX, 0.3f);
        pad.AxisMoved(GamepadAxis.LeftX, 1f);
        pad.AxisMoved(GamepadAxis.LeftX, 0f);

        Assert.True(pad.IsAnalog(GamepadAxis.LeftX));
    }

    // Per axis, not per device: a SideWinder's hat reports -1/0/+1 into the
    // same axes its analog stick uses, and one twist must not make the whole
    // device look analog.
    [Fact]
    public void AnalogIsDecidedPerAxis()
    {
        SoftwareGamepad pad = new(new FakeInput());
        pad.AxisMoved(GamepadAxis.RightX, 0.3f);

        Assert.True(pad.IsAnalog(GamepadAxis.RightX));
        Assert.False(pad.IsAnalog(GamepadAxis.LeftX));
    }

    // The next device to arrive may not be the one that left.
    [Fact]
    public void UnpluggingTheDeviceForgetsWhatItsAxesWere()
    {
        SoftwareGamepad pad = new(new FakeInput());
        pad.Connected();
        pad.AxisMoved(GamepadAxis.LeftX, 0.3f);
        pad.Disconnected();

        Assert.False(pad.IsAnalog(GamepadAxis.LeftX));
    }

    // Both implementations of the interface must behave the same, so every
    // contract test runs against each. A fresh instance per case keeps the
    // one-shot reads of one test out of the next.
    private static IGamepad Create(bool software)
        => software ? new SoftwareGamepad(new FakeInput()) : new FakeGamepad();

    private sealed class RecordingInput : IInput
    {
        public IGamepadSink? Gamepad { get; private set; }

        public int PollCount { get; private set; }

        public void Poll() => PollCount++;

        public void Register(IKeyboardSink keyboard)
        {
        }

        public void Register(IGamepadSink gamepad) => Gamepad = gamepad;
    }
}

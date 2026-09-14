// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Fakes.Input;

namespace SharpKind.Input.Tests;

public class IGamepadTests
{
    // Writes now name the device they came from, so every test that presses
    // a button attaches one first - which is what the backend does, since
    // SDL opens a device before reporting anything it did.
    private const int DeviceA = 1;
    private const int DeviceB = 2;

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
        sink.Connected(DeviceA, "test device");

        sink.ButtonDown(DeviceA, GamepadButton.A);

        Assert.True(pad.IsPressed(GamepadButton.A));
        Assert.False(pad.IsPressed(GamepadButton.A)); // one-shot: consumed
        Assert.True(pad.IsHeld(GamepadButton.A));
        Assert.True(pad.IsHeld(GamepadButton.A)); // continuous: not consumed

        sink.ButtonUp(DeviceA, GamepadButton.A);

        Assert.False(pad.IsHeld(GamepadButton.A));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoneButtonIsNeverPressedOrHeld(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;
        sink.Connected(DeviceA, "test device");

        sink.ButtonDown(DeviceA, GamepadButton.None);

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
        sink.Connected(DeviceA, "test device");

        sink.AxisMoved(DeviceA, GamepadAxis.LeftX, -0.42f);

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
        sink.Connected(DeviceA, "test device");

        sink.AxisMoved(DeviceA, GamepadAxis.LeftX, -1f);
        Assert.Equal(-1f, pad.Axis(GamepadAxis.LeftX));

        sink.AxisMoved(DeviceA, GamepadAxis.LeftX, 0f);
        Assert.Equal(0f, pad.Axis(GamepadAxis.LeftX));

        sink.AxisMoved(DeviceA, GamepadAxis.LeftX, 1f);
        Assert.Equal(1f, pad.Axis(GamepadAxis.LeftX));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AxisIsClampedToTheDocumentedRange(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;
        sink.Connected(DeviceA, "test device");

        sink.AxisMoved(DeviceA, GamepadAxis.LeftX, -3f);
        Assert.Equal(-1f, pad.Axis(GamepadAxis.LeftX));

        sink.AxisMoved(DeviceA, GamepadAxis.LeftX, 3f);
        Assert.Equal(1f, pad.Axis(GamepadAxis.LeftX));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConnectedTracksAttachedDevicesAcrossHotplug(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;

        sink.Connected(DeviceA, "test device");
        Assert.True(pad.IsConnected);

        sink.Connected(DeviceB, "second device");
        sink.Disconnected(DeviceB);
        Assert.True(pad.IsConnected); // the first is still attached

        sink.Disconnected(DeviceA);
        Assert.False(pad.IsConnected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnplugClearsHeldStateSoNothingStaysStuck(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;

        sink.Connected(DeviceA, "test device");
        sink.ButtonDown(DeviceA, GamepadButton.A);
        sink.AxisMoved(DeviceA, GamepadAxis.LeftX, -1f);

        sink.Disconnected(DeviceA);

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
        sink.Connected(DeviceA, "test device");

        sink.ButtonDown(DeviceA, GamepadButton.A);
        sink.ButtonDown(DeviceA, GamepadButton.B);
        sink.AxisMoved(DeviceA, GamepadAxis.RightTrigger, 1f);

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
        pad.Connected(DeviceA, "test device");
        pad.AxisMoved(DeviceA, GamepadAxis.LeftX, value);

        Assert.False(pad.IsAnalog(GamepadAxis.LeftX));
    }

    [Fact]
    public void AnAxisReportingAPositionBetweenIsAnalog()
    {
        SoftwareGamepad pad = new(new FakeInput());
        pad.Connected(DeviceA, "test device");
        pad.AxisMoved(DeviceA, GamepadAxis.LeftX, 0.3f);

        Assert.True(pad.IsAnalog(GamepadAxis.LeftX));
    }

    // A run of extremes is only the stick being at an end, so it cannot
    // withdraw a conclusion the intermediate reading already proved.
    [Fact]
    public void AnAnalogAxisStaysAnalogAtItsExtremes()
    {
        SoftwareGamepad pad = new(new FakeInput());
        pad.Connected(DeviceA, "test device");
        pad.AxisMoved(DeviceA, GamepadAxis.LeftX, 0.3f);
        pad.AxisMoved(DeviceA, GamepadAxis.LeftX, 1f);
        pad.AxisMoved(DeviceA, GamepadAxis.LeftX, 0f);

        Assert.True(pad.IsAnalog(GamepadAxis.LeftX));
    }

    // Per axis, not per device: a SideWinder's hat reports -1/0/+1 into the
    // same axes its analog stick uses, and one twist must not make the whole
    // device look analog.
    [Fact]
    public void AnalogIsDecidedPerAxis()
    {
        SoftwareGamepad pad = new(new FakeInput());
        pad.Connected(DeviceA, "test device");
        pad.AxisMoved(DeviceA, GamepadAxis.RightX, 0.3f);

        Assert.True(pad.IsAnalog(GamepadAxis.RightX));
        Assert.False(pad.IsAnalog(GamepadAxis.LeftX));
    }

    // The next device to arrive may not be the one that left.
    [Fact]
    public void UnpluggingTheDeviceForgetsWhatItsAxesWere()
    {
        SoftwareGamepad pad = new(new FakeInput());
        pad.Connected(DeviceA, "test device");
        pad.AxisMoved(DeviceA, GamepadAxis.LeftX, 0.3f);
        pad.Disconnected(DeviceA);

        Assert.False(pad.IsAnalog(GamepadAxis.LeftX));
    }

    // Two sticks attached, and only the one being flown answers. Merged,
    // a second device resting off-centre fought the one in the hand.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnlyTheActiveDeviceIsRead(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;
        sink.Connected(DeviceA, "first");
        sink.Connected(DeviceB, "second");

        sink.ButtonDown(DeviceB, GamepadButton.A);
        sink.AxisMoved(DeviceB, GamepadAxis.LeftX, 1f);

        Assert.False(pad.IsHeld(GamepadButton.A));
        Assert.Equal(0f, pad.Axis(GamepadAxis.LeftX));

        pad.PreferredDevice = "second";

        Assert.True(pad.IsHeld(GamepadButton.A));
        Assert.Equal(1f, pad.Axis(GamepadAxis.LeftX));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheFirstAttachedDeviceIsActiveByDefault(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;
        sink.Connected(DeviceA, "first");
        sink.Connected(DeviceB, "second");

        Assert.Equal("first", pad.DeviceName);
    }

    // A stick left at home must not leave the commander with no stick at
    // all, so an unattached preference falls back rather than going quiet.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APreferenceForSomethingUnattachedFallsBack(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;
        sink.Connected(DeviceA, "first");
        pad.PreferredDevice = "a stick that is not plugged in";

        Assert.Equal("first", pad.DeviceName);
    }

    // The preference is a wish about whatever may be plugged in later, so
    // the named stick takes over the moment it arrives.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThePreferredDeviceTakesOverWhenItArrives(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;
        pad.PreferredDevice = "second";
        sink.Connected(DeviceA, "first");
        Assert.Equal("first", pad.DeviceName);

        sink.Connected(DeviceB, "second");

        Assert.Equal("second", pad.DeviceName);
    }

    // Unplugging the active device falls back to what is left, rather than
    // leaving reads answering for a stick that has gone.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnpluggingTheActiveDeviceFallsBackToTheOther(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;
        sink.Connected(DeviceA, "first");
        sink.Connected(DeviceB, "second");

        sink.Disconnected(DeviceA);

        Assert.Equal("second", pad.DeviceName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AttachedDevicesListsThemInArrivalOrder(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;
        sink.Connected(DeviceA, "first");
        sink.Connected(DeviceB, "second");

        Assert.Equal(["first", "second"], pad.AttachedDevices);

        sink.Disconnected(DeviceA);

        Assert.Equal(["second"], pad.AttachedDevices);
    }

    // What one stick reports must not reach another's state, or a hat on
    // one would press a button on the other.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DevicesDoNotShareState(bool software)
    {
        IGamepad pad = Create(software);
        IGamepadSink sink = (IGamepadSink)pad;
        sink.Connected(DeviceA, "first");
        sink.Connected(DeviceB, "second");

        sink.AxisMoved(DeviceB, GamepadAxis.LeftX, 0.3f);

        Assert.False(pad.IsAnalog(GamepadAxis.LeftX));

        pad.PreferredDevice = "second";

        Assert.True(pad.IsAnalog(GamepadAxis.LeftX));
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

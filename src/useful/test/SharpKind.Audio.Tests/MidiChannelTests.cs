// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.Midi;

namespace SharpKind.Audio.Tests;

public sealed class MidiChannelTests
{
    [Fact]
    public void ANewChannelHasTheGeneralMidiDefaults()
    {
        MidiChannel channel = new(0);

        Assert.Equal(MathF.Pow(100 / 127f, 2), channel.Gain, 6);
        Assert.Equal(0f, channel.Pan);
        Assert.Equal(0, channel.BendSemitones);
        Assert.Equal(0, channel.Bank);
        Assert.False(channel.Sustain);
    }

    [Fact]
    public void VolumeAndExpressionBothScaleTheGain()
    {
        MidiChannel channel = new(0);

        channel.ControlChange(7, 127);
        channel.ControlChange(11, 64);

        Assert.Equal(MathF.Pow(64 / 127f, 2), channel.Gain, 6);
    }

    [Theory]
    [InlineData(0, -1f)]
    [InlineData(64, 0f)]
    [InlineData(96, 0.5f)]
    public void PanFollowsController10(int value, float expected)
    {
        MidiChannel channel = new(0);

        channel.ControlChange(10, value);

        Assert.Equal(expected, channel.Pan);
    }

    [Fact]
    public void FullBendIsTwoSemitonesByDefault()
    {
        MidiChannel channel = new(0);

        channel.PitchBend(0x7F, 0x7F);

        Assert.Equal(2, channel.BendSemitones, 3);
    }

    [Fact]
    public void RegisteredParameterZeroSetsTheBendRange()
    {
        MidiChannel channel = new(0);

        channel.ControlChange(101, 0);
        channel.ControlChange(100, 0);
        channel.ControlChange(6, 12);
        channel.ControlChange(38, 50);
        channel.PitchBend(0, 0);

        Assert.Equal(-12.5, channel.BendSemitones);
    }

    [Fact]
    public void DataEntryWithoutTheBendRangeSelectedIsIgnored()
    {
        MidiChannel channel = new(0);

        channel.ControlChange(6, 12);
        channel.PitchBend(0, 0);

        Assert.Equal(-2, channel.BendSemitones);
    }

    [Fact]
    public void ResetControllersRestoresExpressionBendAndSustainButNotVolume()
    {
        MidiChannel channel = new(0);
        channel.ControlChange(7, 127);
        channel.ControlChange(11, 0);
        channel.ControlChange(64, 127);
        channel.PitchBend(0, 0);

        channel.ControlChange(121, 0);

        Assert.Equal(1f, channel.Gain);
        Assert.Equal(0, channel.BendSemitones);
        Assert.False(channel.Sustain);
    }

    [Fact]
    public void ControllerZeroSelectsTheBank()
    {
        MidiChannel channel = new(0);

        channel.ControlChange(0, 8);

        Assert.Equal(8, channel.Bank);
    }

    [Fact]
    public void TheTenthChannelIsPercussion()
    {
        MidiChannel channel = new(9);

        channel.ControlChange(0, 8);

        Assert.Equal(128, channel.Bank);
    }
}

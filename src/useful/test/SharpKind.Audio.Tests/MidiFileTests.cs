// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.Midi;
using static SharpKind.Audio.Tests.AudioFixtures;

namespace SharpKind.Audio.Tests;

public sealed class MidiFileTests
{
    private static readonly byte[] s_endOfTrack = [0x00, 0xFF, 0x2F, 0x00];

    [Fact]
    public void TheMinimalFileHoldsANoteForAQuarterOfASecond()
    {
        MidiFile file = MidiFile.Parse(BuildMinimalMidiFile(), "minimal");

        Assert.Equal([new MidiEvent(0, 0, 0x90, 60, 100), new MidiEvent(11025, 0, 0x80, 60, 64)], file.Events);
        Assert.Equal(11025, file.Length);
    }

    // At 120 bpm for a beat, then 240 bpm: the second beat lasts a quarter of a second.
    [Fact]
    public void ATempoChangeOnAnotherTrackRetimesTheRest()
    {
        byte[] tempo =
        [
            0x00, 0xFF, 0x51, 0x03, 0x07, 0xA1, 0x20, // delta 0, 500000 us a beat
            0x83, 0x60, 0xFF, 0x51, 0x03, 0x03, 0xD0, 0x90, // delta 480, 250000 us a beat
            .. s_endOfTrack,
        ];
        byte[] notes =
        [
            0x00, 0x91, 0x40, 0x50, // delta 0, note on ch1
            0x87, 0x40, 0x81, 0x40, 0x00, // delta 960, note off ch1
            .. s_endOfTrack,
        ];

        MidiFile file = MidiFile.Parse(BuildMidiFile(format: 1, division: 480, tempo, notes), "tempo");

        Assert.Equal([0, 33075], file.Events.Select(e => e.SampleTime));
        Assert.All(file.Events, e => Assert.Equal(1, e.Channel));
        Assert.Equal(33075, file.Length);
    }

    [Fact]
    public void RunningStatusRepeatsTheLastStatus()
    {
        byte[] track = [0x00, 0x92, 0x3C, 0x64, 0x00, 0x3E, 0x65, 0x00, 0xC2, 0x05, 0x00, 0x06, .. s_endOfTrack];

        MidiFile file = MidiFile.Parse(BuildMidiFile(format: 0, division: 480, track), "running");

        Assert.Equal(
            [
                new MidiEvent(0, 2, 0x90, 60, 100),
                new MidiEvent(0, 2, 0x90, 62, 101),
                new MidiEvent(0, 2, 0xC0, 5, 0),
                new MidiEvent(0, 2, 0xC0, 6, 0),
            ],
            file.Events);
    }

    [Fact]
    public void SysexAndOtherMetaEventsAreSkipped()
    {
        byte[] track = [0x00, 0xF0, 0x02, 0x7E, 0xF7, 0x00, 0xFF, 0x03, 0x02, 0x41, 0x42, 0x00, 0x90, 0x3C, 0x64, .. s_endOfTrack];

        MidiFile file = MidiFile.Parse(BuildMidiFile(format: 0, division: 480, track), "skip");

        Assert.Equal([new MidiEvent(0, 0, 0x90, 60, 100)], file.Events);
    }

    [Fact]
    public void SmpteTimingThrows()
    {
        byte[] data = BuildMidiFile(format: 0, division: unchecked((short)0xE728), s_endOfTrack);

        Assert.Throws<SharpKindException>(() => MidiFile.Parse(data, "smpte"));
    }

    [Fact]
    public void FormatTwoThrows()
        => Assert.Throws<SharpKindException>(() => MidiFile.Parse(BuildMidiFile(format: 2, division: 480, s_endOfTrack), "format2"));

    [Fact]
    public void ATruncatedFileThrowsSharpKindException()
    {
        byte[] data = BuildMinimalMidiFile()[..^6];

        Assert.Throws<SharpKindException>(() => MidiFile.Parse(data, "truncated"));
    }
}

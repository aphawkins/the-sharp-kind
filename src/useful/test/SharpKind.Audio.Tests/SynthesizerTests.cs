// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.Midi;
using static SharpKind.Audio.Tests.AudioFixtures;

namespace SharpKind.Audio.Tests;

public sealed class SynthesizerTests
{
    [Fact]
    public void MoreNotesThanVoicesNeverThrowAndAllSound()
    {
        Synthesizer synthesizer = new(ReadSoundFont(BuildMinimalSoundFont()));

        for (int key = 0; key <= Synthesizer.VoiceCount; key++)
        {
            synthesizer.NoteOn(0, key, 100);
        }

        Assert.Equal(Synthesizer.VoiceCount, synthesizer.ActiveVoiceCount);
        Assert.Contains(Render(synthesizer, 1000), s => Math.Abs(s) > 0.001f);
    }

    [Fact]
    public void ANoteOfTheSameExclusiveClassCutsTheOldOne()
    {
        byte[] font = BuildSoundFont([(0, 0, [[(GenInstrument, 0)]])], [[(GenExclusiveClass, 1), (GenSampleId, 0)]]);
        Synthesizer synthesizer = new(ReadSoundFont(font));

        synthesizer.NoteOn(0, 60, 100);
        synthesizer.NoteOn(0, 62, 100);

        Assert.Equal(1, synthesizer.ActiveVoiceCount);
    }

    [Fact]
    public void SustainHoldsANoteUntilThePedalLifts()
    {
        Synthesizer synthesizer = new(ReadSoundFont(LoopedFont()));

        synthesizer.ProcessMessage(0, 0xB0, 64, 127);
        synthesizer.NoteOn(0, 60, 100);
        synthesizer.NoteOff(0, 60);
        Render(synthesizer, 1000);

        Assert.Equal(1, synthesizer.ActiveVoiceCount);

        synthesizer.ProcessMessage(0, 0xB0, 64, 0);
        Render(synthesizer, 1000);

        Assert.Equal(0, synthesizer.ActiveVoiceCount);
    }

    [Fact]
    public void NoteOnWithVelocityZeroIsANoteOff()
    {
        Synthesizer synthesizer = new(ReadSoundFont(LoopedFont()));

        synthesizer.NoteOn(0, 60, 100);
        synthesizer.NoteOn(0, 60, 0);
        Render(synthesizer, 1000);

        Assert.Equal(0, synthesizer.ActiveVoiceCount);
    }

    [Fact]
    public void TheTenthChannelPlaysBank128()
    {
        byte[] font = BuildSoundFont([(128, 0, [[(GenInstrument, 0)]])], [[(GenSampleId, 0)]]);
        Synthesizer synthesizer = new(ReadSoundFont(font));

        synthesizer.NoteOn(0, 60, 100);
        Assert.Equal(0, synthesizer.ActiveVoiceCount);

        synthesizer.NoteOn(9, 60, 100);
        Assert.Equal(1, synthesizer.ActiveVoiceCount);
    }

    [Fact]
    public void AllSoundOffSilencesAtOnce()
    {
        Synthesizer synthesizer = new(ReadSoundFont(LoopedFont()));
        synthesizer.NoteOn(0, 60, 100);

        synthesizer.ProcessMessage(0, 0xB0, 120, 0);

        Assert.Equal(0, synthesizer.ActiveVoiceCount);
    }

    // The minimal font with its sample looped, so a held note never runs out.
    private static byte[] LoopedFont()
        => BuildSoundFont([(0, 0, [[(GenInstrument, 0)]])], [[(GenSampleModes, 1), (GenSampleId, 0)]]);

    private static float[] Render(Synthesizer synthesizer, int frames)
    {
        float[] buffer = new float[frames * 2];
        synthesizer.Render(buffer);
        return buffer;
    }
}

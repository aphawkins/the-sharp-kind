// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.Midi;
using static SharpKind.Audio.Tests.AudioFixtures;

namespace SharpKind.Audio.Tests;

public sealed class MidiSequencerTests
{
    private const int Quarter = 11025;

    // The minimal file holds its note for 0.25 s; repeating, it starts it again every 0.25 s.
    [Fact]
    public void ARepeatingSequenceSoundsInEveryPeriod()
    {
        float[] buffer = Render(repeat: true, frames: 4 * Quarter);

        for (int period = 0; period < 4; period++)
        {
            Assert.True(Rms(buffer.AsSpan(period * Quarter * 2, Quarter * 2)) > 0.01f, $"Period {period} is silent");
        }
    }

    [Fact]
    public void ASequenceThatDoesNotRepeatFallsSilentAndEnds()
    {
        MidiSequencer sequencer = Sequencer(repeat: false);
        float[] buffer = new float[4 * Quarter * 2];
        sequencer.Render(buffer);

        Assert.Equal(0f, Rms(buffer.AsSpan(2 * Quarter * 2)));
        Assert.True(sequencer.EndOfSequence);
        Assert.Equal(0, sequencer.Synthesizer.ActiveVoiceCount);
    }

    [Fact]
    public void RenderingInSmallBlocksMatchesOneBigBlock()
    {
        float[] whole = Render(repeat: true, frames: 2 * Quarter);

        MidiSequencer sequencer = Sequencer(repeat: true);
        float[] blocks = new float[whole.Length];
        for (int offset = 0; offset < blocks.Length; offset += 2 * 97)
        {
            sequencer.Render(blocks.AsSpan(offset, Math.Min(2 * 97, blocks.Length - offset)));
        }

        Assert.Equal(whole, blocks);
    }

    private static MidiSequencer Sequencer(bool repeat)
        => new(MidiFile.Parse(BuildMinimalMidiFile(), "minimal"), new Synthesizer(ReadSoundFont(BuildMinimalSoundFont())), repeat);

    private static float[] Render(bool repeat, int frames)
    {
        float[] buffer = new float[frames * 2];
        Sequencer(repeat).Render(buffer);
        return buffer;
    }

    private static float Rms(ReadOnlySpan<float> samples)
    {
        double sum = 0;
        foreach (float s in samples)
        {
            sum += s * s;
        }

        return (float)Math.Sqrt(sum / samples.Length);
    }
}

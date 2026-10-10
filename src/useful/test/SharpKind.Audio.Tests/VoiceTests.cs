// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.Midi;
using SharpKind.Audio.SoundFonts;

namespace SharpKind.Audio.Tests;

public sealed class VoiceTests
{
    private const int SampleLength = 1000;

    // Ten cycles of a sine with a 100-sample period, then the padding a SoundFont puts after every sample.
    private static readonly short[] s_data = [.. Enumerable.Range(0, SampleLength + 46)
        .Select(i => i < SampleLength ? (short)(16000 * Math.Sin(2 * Math.PI * i / 100)) : (short)0)];

    private readonly MidiChannel _channel = new(0);

    private static VoiceParameters Parameters => new(
        SampleStart: 0,
        SampleEnd: SampleLength,
        LoopStart: 0,
        LoopEnd: SampleLength,
        SampleRate: 44100,
        RootKey: 60,
        PitchCorrection: 0,
        CoarseTune: 0,
        FineTune: 0,
        ScaleTuning: 100,
        Pan: 0,
        Attenuation: 0,
        DelayVolEnv: -12000,
        AttackVolEnv: -12000,
        HoldVolEnv: -12000,
        DecayVolEnv: -12000,
        SustainVolEnv: 0,
        ReleaseVolEnv: -12000,
        SampleModes: 0,
        ExclusiveClass: 0);

    [Fact]
    public void ALoopedSampleKeepsSoundingAfterItsLength()
    {
        Voice voice = Start(Parameters with { SampleModes = 1 }, key: 60);
        float[] buffer = Render(voice, 5 * SampleLength);

        Assert.False(voice.IsFinished);
        Assert.Contains(buffer[^200..], s => Math.Abs(s) > 0.01f);
    }

    [Fact]
    public void AnUnloopedSampleStopsAtItsEnd()
    {
        Voice voice = Start(Parameters, key: 60);
        float[] buffer = Render(voice, 5 * SampleLength);

        Assert.True(voice.IsFinished);
        Assert.All(buffer[(4 * SampleLength)..], s => Assert.Equal(0f, s));
    }

    [Fact]
    public void AnOctaveAboveTheRootDoublesTheZeroCrossings()
    {
        int root = ZeroCrossings(Render(Start(Parameters with { SampleModes = 1 }, key: 60), 4410));
        int octave = ZeroCrossings(Render(Start(Parameters with { SampleModes = 1 }, key: 72), 4410));

        Assert.InRange(octave, (2 * root) - 2, (2 * root) + 2);
    }

    // 250 cB asked for is 100 cB applied, 10 dB down.
    [Fact]
    public void InitialAttenuationIsAppliedAtFortyPercent()
    {
        float full = Peak(Render(Start(Parameters with { SampleModes = 1 }, key: 60), 500));
        float attenuated = Peak(Render(Start(Parameters with { Attenuation = 250, SampleModes = 1 }, key: 60), 500));

        Assert.Equal(Math.Pow(10, -0.5), attenuated / full, 3);
    }

    [Fact]
    public void FullLeftPanLeavesTheRightChannelSilent()
    {
        float[] buffer = Render(Start(Parameters with { Pan = -500, SampleModes = 1 }, key: 60), 500);

        Assert.Contains(buffer.Where((_, i) => i % 2 == 0), s => Math.Abs(s) > 0.01f);
        Assert.All(buffer.Where((_, i) => i % 2 != 0), s => Assert.Equal(0f, s, 6));
    }

    private static Voice Start(in VoiceParameters parameters, int key)
    {
        Voice voice = new();
        voice.Start(parameters, s_data, channel: 0, key, velocity: 127, startOrder: 0);
        return voice;
    }

    private static float Peak(float[] stereo) => stereo.Max(Math.Abs);

    private static int ZeroCrossings(float[] stereo)
    {
        int crossings = 0;
        for (int i = 2; i < stereo.Length; i += 2)
        {
            if ((stereo[i - 2] < 0) != (stereo[i] < 0))
            {
                crossings++;
            }
        }

        return crossings;
    }

    private float[] Render(Voice voice, int frames)
    {
        float[] buffer = new float[frames * 2];
        voice.Render(buffer, _channel);
        return buffer;
    }
}

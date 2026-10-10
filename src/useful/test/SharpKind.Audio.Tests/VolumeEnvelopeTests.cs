// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.Midi;
using SharpKind.Audio.SoundFonts;

namespace SharpKind.Audio.Tests;

public sealed class VolumeEnvelopeTests
{
    // About 10 ms.
    private const int AttackTimecents = -7973;

    // The SoundFont defaults: every time about a millisecond, full sustain.
    private static VoiceParameters Parameters => new(
        SampleStart: 0,
        SampleEnd: 0,
        LoopStart: 0,
        LoopEnd: 0,
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
    public void AttackReachesFullGainAfterTheDelayAndAttackTimes()
    {
        VolumeEnvelope envelope = Start(Parameters with { AttackVolEnv = AttackTimecents });
        int expected = VolumeEnvelope.TimecentsToSamples(-12000) + VolumeEnvelope.TimecentsToSamples(AttackTimecents);

        int steps = 0;
        while (envelope.Gain < 1 && steps < 100_000)
        {
            envelope.Step();
            steps++;
        }

        Assert.InRange(steps, expected - 1, expected + 1);
    }

    // 200 cB is 20 dB down, a tenth of full amplitude.
    [Fact]
    public void DecaySettlesAtTheSustainLevel()
    {
        VolumeEnvelope envelope = Start(Parameters with { SustainVolEnv = 200 });

        Step(envelope, 10_000);

        Assert.Equal(0.1, envelope.Gain, 6);
        Assert.False(envelope.IsFinished);
    }

    [Fact]
    public void ReleaseFinishes()
    {
        VolumeEnvelope envelope = Start(Parameters);
        Step(envelope, 1000);

        envelope.Release();
        Step(envelope, 1000);

        Assert.True(envelope.IsFinished);
    }

    [Fact]
    public void NoteOffDuringAttackGoesStraightToRelease()
    {
        VolumeEnvelope envelope = Start(Parameters with { AttackVolEnv = 0 }); // a second
        Step(envelope, 2000);
        double before = envelope.Gain;

        envelope.Release();
        envelope.Step();

        Assert.InRange(before, 0.01, 0.1);
        Assert.True(envelope.Gain < before, $"Gain rose from {before} to {envelope.Gain}");
    }

    private static VolumeEnvelope Start(in VoiceParameters parameters)
    {
        VolumeEnvelope envelope = new();
        envelope.Start(parameters);
        return envelope;
    }

    private static void Step(VolumeEnvelope envelope, int count)
    {
        for (int i = 0; i < count; i++)
        {
            envelope.Step();
        }
    }
}

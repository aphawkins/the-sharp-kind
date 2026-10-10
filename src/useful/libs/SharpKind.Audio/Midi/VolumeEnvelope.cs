// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.SoundFonts;

namespace SharpKind.Audio.Midi;

// A SoundFont volume envelope, stepped once per output sample: delay, a linear attack, hold, then decay and release falling
// at a steady rate in decibels, which is a constant factor per sample.
internal sealed class VolumeEnvelope
{
    // 96 dB down: the envelope has gone silent.
    private const double SilentAttenuation = 960;

    private static readonly double s_silentGain = CentibelsToGain(SilentAttenuation);

    private Stage _stage = Stage.Finished;
    private int _stageSamples;
    private int _stageLength;
    private int _holdLength;
    private double _attackStep;
    private double _decayFactor;
    private double _sustainGain;
    private double _releaseFactor;

    private enum Stage
    {
        Delay = 0,
        Attack = 1,
        Hold = 2,
        Decay = 3,
        Sustain = 4,
        Release = 5,
        Finished = 6,
    }

    internal double Gain { get; private set; }

    internal bool IsFinished => _stage == Stage.Finished;

    internal static double CentibelsToGain(double centibels) => Math.Pow(10, -centibels / 200);

    internal static int TimecentsToSamples(int timecents) => (int)Math.Round(Math.Pow(2, timecents / 1200.0) * SoftwareSound.SampleRate);

    internal void Start(in VoiceParameters parameters)
    {
        _attackStep = 1.0 / Math.Max(1, TimecentsToSamples(parameters.AttackVolEnv));
        _decayFactor = FallFactor(parameters.DecayVolEnv);
        _releaseFactor = FallFactor(parameters.ReleaseVolEnv);
        _sustainGain = CentibelsToGain(Math.Clamp(parameters.SustainVolEnv, 0, 1440));
        _holdLength = TimecentsToSamples(parameters.HoldVolEnv);

        Gain = 0;
        Enter(Stage.Delay, TimecentsToSamples(parameters.DelayVolEnv));
    }

    // Note-off: release from wherever the envelope has got to.
    internal void Release()
    {
        if (_stage != Stage.Finished)
        {
            _stage = Gain <= s_silentGain ? Stage.Finished : Stage.Release;
        }
    }

    internal void Stop()
    {
        _stage = Stage.Finished;
        Gain = 0;
    }

    internal void Step()
    {
        switch (_stage)
        {
            case Stage.Delay:
                Gain = 0;
                if (++_stageSamples >= _stageLength)
                {
                    Enter(Stage.Attack, 0);
                }

                break;

            case Stage.Attack:
                Gain = Math.Min(1, Gain + _attackStep);
                if (Gain >= 1)
                {
                    Enter(Stage.Hold, _holdLength);
                }

                break;

            case Stage.Hold:
                Gain = 1;
                if (++_stageSamples >= _stageLength)
                {
                    Enter(Stage.Decay, 0);
                }

                break;

            case Stage.Decay:
                Gain *= _decayFactor;
                if (Gain <= _sustainGain)
                {
                    Gain = _sustainGain;
                    _stage = Gain <= s_silentGain ? Stage.Finished : Stage.Sustain;
                }

                break;

            case Stage.Release:
                Gain *= _releaseFactor;
                if (Gain <= s_silentGain)
                {
                    Stop();
                }

                break;
        }
    }

    // Decay and release fall 1000 cB over their time, so this much each sample.
    private static double FallFactor(int timecents)
        => CentibelsToGain(1000.0 / Math.Max(1, TimecentsToSamples(timecents)));

    private void Enter(Stage stage, int length)
    {
        _stage = stage;
        _stageSamples = 0;
        _stageLength = length;
    }
}

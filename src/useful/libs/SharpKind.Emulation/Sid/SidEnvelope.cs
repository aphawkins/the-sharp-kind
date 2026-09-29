// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Emulation.Sid;

/// <summary>One SID voice's ADSR envelope, clocked once per CPU cycle. The ADSR "delay bug" is not modelled.</summary>
internal sealed class SidEnvelope
{
    private const int MaxLevel = 0xFF;

    // Cycles per envelope step for each rate setting 0-15.
    private static readonly int[] s_periods = [9, 32, 63, 95, 149, 220, 267, 313, 392, 977, 1954, 3126, 3907, 11720, 19532, 31251];

    private Stage _stage = Stage.Release;
    private int _rateCounter;
    private int _exponentCounter;
    private bool _gate;

    private enum Stage
    {
        Attack = 0,
        Decay = 1,
        Sustain = 2,
        Release = 3,
    }

    internal int AttackRate { get; set; }

    internal int DecayRate { get; set; }

    internal int SustainLevel { get; set; }

    internal int ReleaseRate { get; set; }

    internal int Level { get; private set; }

    internal void SetGate(bool gate)
    {
        if (gate == _gate)
        {
            return;
        }

        _gate = gate;
        _stage = gate ? Stage.Attack : Stage.Release;
        _exponentCounter = 0;
    }

    internal void Clock()
    {
        if (_stage == Stage.Sustain)
        {
            return;
        }

        int rate = _stage switch
        {
            Stage.Attack => AttackRate,
            Stage.Decay => DecayRate,
            _ => ReleaseRate,
        };

        _rateCounter++;

        if (_rateCounter < s_periods[rate])
        {
            return;
        }

        _rateCounter = 0;

        if (_stage == Stage.Attack)
        {
            Level++;

            if (Level >= MaxLevel)
            {
                Level = MaxLevel;
                _stage = Stage.Decay;
                _exponentCounter = 0;
            }

            return;
        }

        _exponentCounter++;

        if (_exponentCounter < ExponentPeriod(Level))
        {
            return;
        }

        _exponentCounter = 0;

        if (_stage == Stage.Release)
        {
            if (Level > 0)
            {
                Level--;
            }

            return;
        }

        int sustain = SustainLevel * 0x11;

        if (Level > sustain)
        {
            Level--;
        }

        if (Level <= sustain)
        {
            _stage = Stage.Sustain;
        }
    }

    // The decay and release slow as the level falls, to approximate an exponential.
    private static int ExponentPeriod(int level) => level switch
    {
        > 93 => 1,
        > 54 => 2,
        > 26 => 4,
        > 14 => 8,
        > 6 => 16,
        _ => 30,
    };
}

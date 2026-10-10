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

    /// <summary>Gets a value indicating whether the level is 0 and stays so until the gate opens.</summary>
    internal bool IsSilent => Level == 0 && _stage != Stage.Attack;

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

    /// <summary>Runs as many cycles as <see cref="Clock"/> would, at once, for a silent envelope.</summary>
    /// <param name="cycles">The cycles to run.</param>
    internal void Advance(int cycles)
    {
        // A level of 0 only ever sustains or releases; anything else is clocked as it comes.
        if (_stage == Stage.Sustain || cycles == 0)
        {
            return;
        }

        if (_stage != Stage.Release || Level != 0)
        {
            for (int i = 0; i < cycles; i++)
            {
                Clock();
            }

            return;
        }

        // A counter already past its period (the rate was written lower) ends its step on the next increment, just as
        // one at period - 1 does.
        int period = s_periods[ReleaseRate];
        int exponentPeriod = ExponentPeriod(0);
        _rateCounter = Math.Min(_rateCounter, period - 1);
        _exponentCounter = Math.Min(_exponentCounter, exponentPeriod - 1);

        // Each step at level 0 only moves the exponent counter on; the level stays where it is.
        int total = _rateCounter + cycles;
        _rateCounter = total % period;
        _exponentCounter = (_exponentCounter + (total / period)) % exponentPeriod;
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

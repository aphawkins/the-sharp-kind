// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Emulation.Sid;

/// <summary>
/// The SID's state-variable filter as a Chamberlin filter, stepped once per CPU cycle. The 6581's cutoff curve is
/// approximated by a straight line.
/// </summary>
internal sealed class SidFilter
{
    internal const byte LowPassBit = 0x10;
    internal const byte BandPassBit = 0x20;
    internal const byte HighPassBit = 0x40;

    // Far below anything audible, and far above the denormal range, where float arithmetic runs many times slower.
    internal const float Inaudible = 1e-20f;

    private const double BaseCutoff = 30.0;
    private const double CutoffPerStep = 5.8;

    private float _low;
    private float _band;
    private float _frequency;
    private float _damping = 1.0f;
    private int _cutoff;
    private int _resonance;

    /// <summary>Gets a value indicating whether the filter holds nothing, so silence in gives exactly silence out.</summary>
    internal bool IsAtRest => _low == 0 && _band == 0;

    /// <summary>Sets the 11-bit cutoff.</summary>
    /// <param name="value">The cutoff register value.</param>
    internal void SetCutoff(int value)
    {
        _cutoff = value & 0x7FF;
        Update();
    }

    /// <summary>Sets the 4-bit resonance.</summary>
    /// <param name="value">The resonance register value.</param>
    internal void SetResonance(int value)
    {
        _resonance = value & 0x0F;
        Update();
    }

    /// <summary>Runs one cycle.</summary>
    /// <param name="input">The sum of the routed voices, in the range -1 to 1.</param>
    /// <param name="mode">The mode bits of register $18 (low, band and high pass).</param>
    /// <returns>The sum of the selected outputs.</returns>
    internal float Clock(float input, byte mode)
    {
        _low += _frequency * _band;

        float high = input - _low - (_damping * _band);

        _band += _frequency * high;

        float output = 0;

        if ((mode & LowPassBit) != 0)
        {
            output += _low;
        }

        if ((mode & BandPassBit) != 0)
        {
            output += _band;
        }

        if ((mode & HighPassBit) != 0)
        {
            output += high;
        }

        return output;
    }

    /// <summary>Lets a filter that has rung down to nothing audible come to rest.</summary>
    internal void Settle()
    {
        if (Math.Abs(_low) < Inaudible && Math.Abs(_band) < Inaudible)
        {
            _low = 0;
            _band = 0;
        }
    }

    private void Update()
    {
        double hertz = BaseCutoff + (_cutoff * CutoffPerStep);

        _frequency = (float)(2.0 * Math.Sin(Math.PI * hertz / SidChip.ClockRate));
        _damping = (float)(1.0 / (0.707 + (_resonance / 15.0 * 1.5)));
    }
}

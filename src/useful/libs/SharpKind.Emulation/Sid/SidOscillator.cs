// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Emulation.Sid;

/// <summary>One SID voice's waveform generator, clocked once per CPU cycle.</summary>
internal sealed class SidOscillator
{
    internal const byte GateBit = 0x01;
    internal const byte SyncBit = 0x02;
    internal const byte RingBit = 0x04;
    internal const byte TestBit = 0x08;
    internal const byte TriangleBit = 0x10;
    internal const byte SawBit = 0x20;
    internal const byte PulseBit = 0x40;
    internal const byte NoiseBit = 0x80;

    private const int AccumulatorMask = 0xFFFFFF;
    private const int TopBit = 0x800000;
    private const int NoiseClockBit = 0x080000;
    private const int NoiseSeed = 0x7FFFF8;
    private const int NoiseMask = 0x7FFFFF;
    private const int Silent = 0;
    private const int FullScale = 0xFFF;

    // The LFSR bits that make up the 8-bit noise output, most significant first.
    private static readonly int[] s_noiseTaps = [22, 20, 16, 13, 11, 7, 4, 2];

    private int _accumulator;
    private int _noise = NoiseSeed;
    private bool _noiseClockWasHigh;

    internal int Frequency { get; set; }

    internal int PulseWidth { get; set; }

    internal byte Control { get; set; }

    /// <summary>Gets a value indicating whether the accumulator's top bit is set.</summary>
    internal bool Msb => (_accumulator & TopBit) != 0;

    /// <summary>Gets a value indicating whether the top bit went from clear to set in the last <see cref="Clock"/>.</summary>
    internal bool MsbRose { get; private set; }

    /// <summary>Sets the control register, as a write to the chip does.</summary>
    /// <param name="value">The new control byte.</param>
    internal void WriteControl(byte value)
    {
        if ((value & TestBit) != 0 && (Control & TestBit) == 0)
        {
            _accumulator = 0;
            _noise = NoiseSeed;
        }

        Control = value;
    }

    internal void Clock()
    {
        bool before = Msb;

        _accumulator = (Control & TestBit) != 0 ? 0 : (_accumulator + Frequency) & AccumulatorMask;

        MsbRose = !before && Msb;

        bool clockHigh = (_accumulator & NoiseClockBit) != 0;

        if (clockHigh && !_noiseClockWasHigh)
        {
            int feedback = ((_noise >> 22) ^ (_noise >> 17)) & 1;
            _noise = ((_noise << 1) | feedback) & NoiseMask;
        }

        _noiseClockWasHigh = clockHigh;
    }

    /// <summary>Restarts the accumulator when this voice is synced and its source has wrapped.</summary>
    /// <param name="sourceRose">Whether the sync source's top bit rose this cycle.</param>
    internal void Synchronise(bool sourceRose)
    {
        if (sourceRose && (Control & SyncBit) != 0)
        {
            _accumulator = 0;
        }
    }

    /// <summary>Gets the 12-bit waveform output.</summary>
    /// <param name="ringSourceMsb">The top bit of the voice that ring-modulates this one.</param>
    /// <returns>The waveform, 0 to 4095.</returns>
    internal int Output(bool ringSourceMsb)
    {
        int selected = Control & (TriangleBit | SawBit | PulseBit | NoiseBit);

        if (selected == 0)
        {
            return Silent;
        }

        int wave = FullScale;

        if ((Control & TriangleBit) != 0)
        {
            bool fold = Msb ^ ((Control & RingBit) != 0 && ringSourceMsb);
            wave &= ((_accumulator ^ (fold ? AccumulatorMask : 0)) >> 11) & FullScale;
        }

        if ((Control & SawBit) != 0)
        {
            wave &= _accumulator >> 12;
        }

        if ((Control & PulseBit) != 0)
        {
            wave &= (Control & TestBit) != 0 || (_accumulator >> 12) >= PulseWidth ? FullScale : 0;
        }

        if ((Control & NoiseBit) != 0)
        {
            wave &= NoiseOutput() << 4;
        }

        return wave;
    }

    private int NoiseOutput()
    {
        int output = 0;

        foreach (int bit in s_noiseTaps)
        {
            output = (output << 1) | ((_noise >> bit) & 1);
        }

        return output;
    }
}

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
    private const int NoiseClockShift = 19;

    // The LFSR bits that make up the 8-bit noise output, most significant first.
    private static readonly int[] s_noiseTaps = [22, 20, 16, 13, 11, 7, 4, 2];

    private int _accumulator;
    private int _noise = NoiseSeed;
    private int _noiseOutput = NoiseOutput(NoiseSeed);
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
            SetNoise(NoiseSeed);
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
            ShiftNoise();
        }

        _noiseClockWasHigh = clockHigh;
    }

    /// <summary>
    /// Runs as many cycles as <see cref="Clock"/> would, at once, for a voice no other voice syncs: the accumulator moves
    /// on by the frequency for each cycle, and the noise shifts once for each time bit 19 rose on the way.
    /// </summary>
    /// <param name="cycles">The cycles to run, at least 1.</param>
    internal void Advance(int cycles)
    {
        // One cycle as Clock runs it leaves the noise clock's last state matching the accumulator, which the count needs.
        Clock();

        if (--cycles == 0 || (Control & TestBit) != 0)
        {
            return;
        }

        // A step is under 2^19, so the accumulator crosses each multiple of 2^19 on its own cycle, and bit 19 rises on
        // entering every odd one. 2^24 holds an even number of them, so counting past the wrap keeps their parity.
        long start = _accumulator;
        long end = start + ((long)Frequency * cycles);
        long first = (start >> NoiseClockShift) + 1;
        long last = end >> NoiseClockShift;
        long rises = ((last + 1) >> 1) - (first >> 1);

        for (long i = 0; i < rises; i++)
        {
            ShiftNoise();
        }

        long previous = end - Frequency;
        MsbRose = (previous & TopBit) == 0 && (end & TopBit) != 0;
        _accumulator = (int)(end & AccumulatorMask);
        _noiseClockWasHigh = (_accumulator & NoiseClockBit) != 0;
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
            wave &= _noiseOutput << 4;
        }

        return wave;
    }

    private static int NoiseOutput(int noise)
    {
        int output = 0;

        foreach (int bit in s_noiseTaps)
        {
            output = (output << 1) | ((noise >> bit) & 1);
        }

        return output;
    }

    private void ShiftNoise()
    {
        int feedback = ((_noise >> 22) ^ (_noise >> 17)) & 1;
        SetNoise(((_noise << 1) | feedback) & NoiseMask);
    }

    // The output taps only change when the register does, so they are worked out then rather than every cycle.
    private void SetNoise(int noise)
    {
        _noise = noise;
        _noiseOutput = NoiseOutput(noise);
    }
}

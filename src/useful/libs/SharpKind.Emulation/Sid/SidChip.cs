// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Emulation.Sid;

/// <summary>
/// The MOS 6581 SID: three voices and a filter, driven by register writes and clocked at the PAL CPU rate. Written from
/// the public register and waveform documentation. Combined waveforms are ANDed, the filter curve is linear and there
/// is no CIA timing.
/// </summary>
public sealed class SidChip
{
    private const int Voices = 3;
    private const int RegisterCount = 0x20;
    private const int VoiceRegisters = 7;
    private const int FilterLow = 0x15;
    private const int FilterHigh = 0x16;
    private const int FilterRouting = 0x17;
    private const int ModeVolume = 0x18;
    private const int Oscillator3 = 0x1B;
    private const int Envelope3 = 0x1C;

    private const float VoiceScale = 1.0f / (0x800 * 0xFF);
    private const float MixScale = 1.0f / Voices;
    private const float HighPass = 1.0f - (float)(2.0 * Math.PI * 16.0 / 44100);

    private const byte VoiceThreeOff = 0x80;
    private const byte VolumeMask = 0x0F;

    private readonly SidOscillator[] _oscillators = [new(), new(), new()];
    private readonly SidEnvelope[] _envelopes = [new(), new(), new()];
    private readonly SidFilter _filter = new();
    private readonly byte[] _registers = new byte[RegisterCount];

    private byte _routing;
    private byte _modeVolume;
    private double _cycleCarry;

    private float _dcInput;
    private float _dcOutput;

    /// <summary>Gets the PAL CPU clock, in cycles a second.</summary>
    public static double ClockRate => 985248.0;

    /// <summary>Gets the rate <see cref="Render"/> produces frames at.</summary>
    public static int SampleRate => 44100;

    /// <summary>Gets the cycles the chip has been clocked.</summary>
    public long Cycles { get; private set; }

    /// <summary>Gets the envelope level of a voice, 0 to 255.</summary>
    /// <param name="voice">The voice, 0 to 2.</param>
    /// <returns>The level.</returns>
    public int EnvelopeLevel(int voice) => _envelopes[voice].Level;

    /// <summary>Writes a register. The effect is immediate.</summary>
    /// <param name="register">The register, 0 to 31; higher addresses mirror.</param>
    /// <param name="value">The byte written.</param>
    public void Write(int register, byte value)
    {
        int reg = register & (RegisterCount - 1);

        _registers[reg] = value;

        if (reg < Voices * VoiceRegisters)
        {
            WriteVoice(reg / VoiceRegisters, reg % VoiceRegisters, value);
            return;
        }

        switch (reg)
        {
            case FilterLow:
            case FilterHigh:
                _filter.SetCutoff((_registers[FilterLow] & 0x07) | (_registers[FilterHigh] << 3));
                break;
            case FilterRouting:
                _routing = (byte)(value & 0x07);
                _filter.SetResonance(value >> 4);
                break;
            case ModeVolume:
                _modeVolume = value;
                break;
        }
    }

    /// <summary>Reads a register. Only the third voice's waveform and envelope can be read.</summary>
    /// <param name="register">The register, 0 to 31.</param>
    /// <returns>The byte read.</returns>
    public byte Read(int register) => (register & (RegisterCount - 1)) switch
    {
        Oscillator3 => (byte)(_oscillators[2].Output(_oscillators[1].Msb) >> 4),
        Envelope3 => (byte)_envelopes[2].Level,
        _ => 0,
    };

    /// <summary>Renders stereo frames at <see cref="SampleRate"/>, clocking the chip as it goes.</summary>
    /// <param name="stereo">Interleaved left and right samples to fill.</param>
    public void Render(Span<float> stereo)
    {
        for (int frame = 0; frame + 1 < stereo.Length; frame += 2)
        {
            _cycleCarry += ClockRate / SampleRate;

            int cycles = (int)_cycleCarry;

            _cycleCarry -= cycles;

            float sum = IsSilent() ? ClockSilent(cycles) : ClockSounding(cycles);
            float sample = sum / cycles;
            float blocked = sample - _dcInput + (HighPass * _dcOutput);

            // The DC blocker and the filter decay towards zero for ever; they are stopped once nothing can be heard.
            if (sample == 0 && Math.Abs(blocked) < SidFilter.Inaudible)
            {
                blocked = 0;
            }

            _filter.Settle();
            _dcInput = sample;
            _dcOutput = blocked;
            stereo[frame] = blocked;
            stereo[frame + 1] = blocked;
        }
    }

    /// <summary>Clocks the chip a number of cycles without producing output.</summary>
    /// <param name="cycles">The cycles to run.</param>
    public void Advance(int cycles)
    {
        for (int i = 0; i < cycles; i++)
        {
            Clock();
        }
    }

    private void WriteVoice(int voice, int register, byte value)
    {
        SidOscillator oscillator = _oscillators[voice];
        SidEnvelope envelope = _envelopes[voice];

        switch (register)
        {
            case 0:
                oscillator.Frequency = (oscillator.Frequency & 0xFF00) | value;
                break;
            case 1:
                oscillator.Frequency = (oscillator.Frequency & 0x00FF) | (value << 8);
                break;
            case 2:
                oscillator.PulseWidth = (oscillator.PulseWidth & 0x0F00) | value;
                break;
            case 3:
                oscillator.PulseWidth = (oscillator.PulseWidth & 0x00FF) | ((value & 0x0F) << 8);
                break;
            case 4:
                oscillator.WriteControl(value);
                envelope.SetGate((value & SidOscillator.GateBit) != 0);
                break;
            case 5:
                envelope.AttackRate = value >> 4;
                envelope.DecayRate = value & 0x0F;
                break;
            default:
                envelope.SustainLevel = value >> 4;
                envelope.ReleaseRate = value & 0x0F;
                break;
        }
    }

    // Every voice is at level 0, so each puts out exactly 0 until a register is written, and no voice is synced, so none
    // moves another's accumulator.
    private bool IsSilent()
    {
        for (int voice = 0; voice < Voices; voice++)
        {
            if (!_envelopes[voice].IsSilent || (_oscillators[voice].Control & SidOscillator.SyncBit) != 0)
            {
                return false;
            }
        }

        return true;
    }

    // The voices move on a sample's cycles at once; only the filter, still ringing from what was played, is stepped, and
    // its sum is worked out exactly as Clock works it out.
    private float ClockSilent(int cycles)
    {
        Cycles += cycles;

        for (int voice = 0; voice < Voices; voice++)
        {
            _oscillators[voice].Advance(cycles);
            _envelopes[voice].Advance(cycles);
        }

        if (_filter.IsAtRest)
        {
            return 0;
        }

        float sum = 0;

        for (int cycle = 0; cycle < cycles; cycle++)
        {
            float mixed = 0f + _filter.Clock(0f, _modeVolume);
            sum += mixed * MixScale * (_modeVolume & VolumeMask) / VolumeMask;
        }

        return sum;
    }

    private float ClockSounding(int cycles)
    {
        float sum = 0;

        for (int cycle = 0; cycle < cycles; cycle++)
        {
            sum += Clock();
        }

        return sum;
    }

    // Each voice is synced and ring-modulated by the one before it: voice 1 by voice 3, voice 2 by 1, voice 3 by 2.
    private float Clock()
    {
        Cycles++;

        SidOscillator first = _oscillators[0];
        SidOscillator second = _oscillators[1];
        SidOscillator third = _oscillators[2];

        first.Clock();
        second.Clock();
        third.Clock();

        for (int voice = 0; voice < Voices; voice++)
        {
            _envelopes[voice].Clock();
        }

        first.Synchronise(third.MsbRose);
        second.Synchronise(first.MsbRose);
        third.Synchronise(second.MsbRose);

        float filtered = 0;
        float direct = 0;

        Mix(0, first.Output(third.Msb), ref filtered, ref direct);
        Mix(1, second.Output(first.Msb), ref filtered, ref direct);
        Mix(2, third.Output(second.Msb), ref filtered, ref direct);

        float mixed = direct + _filter.Clock(filtered, _modeVolume);

        return mixed * MixScale * (_modeVolume & VolumeMask) / VolumeMask;
    }

    private void Mix(int voice, int wave, ref float filtered, ref float direct)
    {
        float level = (wave - 0x800) * _envelopes[voice].Level * VoiceScale;

        if ((_routing & (1 << voice)) != 0)
        {
            filtered += level;
        }
        else if (voice != 2 || (_modeVolume & VoiceThreeOff) == 0)
        {
            direct += level;
        }
    }
}

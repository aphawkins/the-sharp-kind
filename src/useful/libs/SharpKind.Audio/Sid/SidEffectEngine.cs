// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.Sid;

// C64 Elite's sound-effect engine (elite-source.asm): NOISE and NOISE2 start an effect on one of the three SID voices, SOINT
// runs every voice's effect once a frame, and SOFLUSH ends them all. The arrays are the C64's SOFLG, SOCNT, SOPR, SOFRCH,
// SOFRQ, SOCR, SOATK, SOSUS and SOVCH, one entry a voice; every sum wraps at 8 bits as it does there.
internal sealed class SidEffectEngine(Action<int, byte> writeRegister)
{
    private const int Voices = 3;
    private const int RegistersPerVoice = 7;
    private const byte NewEffect = 0x80;

    private readonly byte[] _flag = new byte[Voices];
    private readonly byte[] _counter = new byte[Voices];
    private readonly byte[] _priority = new byte[Voices];
    private readonly sbyte[] _frequencyChange = new sbyte[Voices];
    private readonly byte[] _frequency = new byte[Voices];
    private readonly byte[] _control = new byte[Voices];
    private readonly byte[] _attackDecay = new byte[Voices];
    private readonly byte[] _sustainRelease = new byte[Voices];
    private readonly byte[] _volumeMask = new byte[Voices];
    private readonly List<(SidEffectStep Step, int Frames)> _delayed = [];

    // PULSEW: the pulse width's high byte, which flips between 2 and 6 every frame.
    private byte _pulseWidth = 2;

    internal void Trigger(IReadOnlyList<SidEffectStep> sound)
    {
        foreach (SidEffectStep step in sound)
        {
            if (step.DelayFrames == 0)
            {
                Start(step);
            }
            else
            {
                _delayed.Add((step, step.DelayFrames));
            }
        }
    }

    // SOINT: voices 3 down to 1, then the pulse width, then any delayed step whose wait is over.
    internal void Tick()
    {
        for (int voice = Voices - 1; voice >= 0; voice--)
        {
            TickVoice(voice);
        }

        _pulseWidth ^= 4;

        int i = 0;
        while (i < _delayed.Count)
        {
            (SidEffectStep step, int frames) = _delayed[i];
            if (frames > 1)
            {
                _delayed[i++] = (step, frames - 1);
            }
            else
            {
                _delayed.RemoveAt(i);
                Start(step);
            }
        }
    }

    // SOFLUSH: every voice's effect ends on the next tick. Delayed steps are dropped too.
    internal void Flush()
    {
        Array.Fill(_counter, (byte)1);
        _delayed.Clear();
    }

    // NOISE, or NOISE2 when the step overrides the frequency and sustain/release.
    private void Start(in SidEffectStep step)
    {
        SidEffect effect = step.Effect;
        int voice = -1;

        // An effect with an even priority takes back the voice it is already playing on.
        if (!step.Layer && (effect.Priority & 1) == 0)
        {
            voice = Array.FindLastIndex(_flag, flag => (flag & 0x3F) == effect.Index + 1);
        }

        if (voice < 0)
        {
            voice = LowestPriorityVoice();
        }

        if (effect.Priority < _priority[voice])
        {
            return;
        }

        _priority[voice] = effect.Priority;
        _sustainRelease[voice] = step.SustainRelease ?? effect.SustainRelease;
        _counter[voice] = effect.Frames;
        _frequencyChange[voice] = effect.FrequencyChange;
        _control[voice] = effect.Control;
        _frequency[voice] = step.Frequency ?? effect.Frequency;
        _attackDecay[voice] = effect.AttackDecay;
        _volumeMask[voice] = effect.VolumeMask;
        _flag[voice] = (byte)((effect.Index + 1) | NewEffect);
    }

    // SOUX9: ties go to the higher voice, so the first effect on an idle chip plays on voice 3.
    private int LowestPriorityVoice()
    {
        int voice = 0;
        byte priority = _priority[0];

        if (_priority[0] >= _priority[1])
        {
            voice = 1;
            priority = _priority[1];
        }

        return priority < _priority[2] ? voice : 2;
    }

    private void TickVoice(int voice)
    {
        if (_flag[voice] == 0)
        {
            return;
        }

        if ((_flag[voice] & NewEffect) != 0)
        {
            for (int register = 0; register < RegistersPerVoice; register++)
            {
                Write(voice, register, 0);
            }

            Write(voice, 4, _control[voice]);
            Write(voice, 5, _attackDecay[voice]);
            Write(voice, 6, _sustainRelease[voice]);
            WriteFrequency(voice, 0);
            _flag[voice] &= 0x7F;
            return;
        }

        if (_frequencyChange[voice] != 0)
        {
            WriteFrequency(voice, _frequencyChange[voice]);
        }

        // Effects lose priority as they play out, but never down to nothing.
        _priority[voice]--;
        if (_priority[voice] == 0)
        {
            _priority[voice] = 1;
        }

        _counter[voice]--;
        if (_counter[voice] == 0)
        {
            Write(voice, 4, (byte)(_control[voice] & 0xFE));
            _flag[voice] = 0;
            _priority[voice] = 0;
            return;
        }

        if ((_counter[voice] & _volumeMask[voice]) == 0)
        {
            _sustainRelease[voice] -= 16;
            Write(voice, 6, _sustainRelease[voice]);
        }
    }

    // The 8-bit frequency is the top byte of the SID's 16-bit one, shifted down two: 00ffffff ff000000.
    private void WriteFrequency(int voice, int change)
    {
        _frequency[voice] = (byte)(_frequency[voice] + change);
        Write(voice, 1, (byte)(_frequency[voice] >> 2));
        Write(voice, 0, (byte)(_frequency[voice] << 6));
        Write(voice, 3, _pulseWidth);
    }

    private void Write(int voice, int register, byte value) => writeRegister((voice * RegistersPerVoice) + register, value);
}

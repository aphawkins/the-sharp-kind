// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.SoundFonts;

namespace SharpKind.Audio.Midi;

// A 16-channel General MIDI synthesizer over a SoundFont, with a fixed pool of voices.
internal sealed class Synthesizer
{
    internal const int VoiceCount = 64;

    // Leaves headroom for several loud notes at once; set to match the level MeltySynth gave Elite's two tunes.
    private const float MasterGain = 0.57f;

    private readonly SoundFont _soundFont;
    private readonly MidiChannel[] _channels = new MidiChannel[16];
    private readonly Voice[] _voices = new Voice[VoiceCount];
    private long _startOrder;

    internal Synthesizer(SoundFont soundFont)
    {
        _soundFont = soundFont;

        for (int i = 0; i < _channels.Length; i++)
        {
            _channels[i] = new MidiChannel(i);
        }

        for (int i = 0; i < _voices.Length; i++)
        {
            _voices[i] = new Voice();
        }
    }

    internal int ActiveVoiceCount => _voices.Count(v => !v.IsFinished);

    internal MidiChannel Channel(int channel) => _channels[channel];

    // A channel message: the command is the status byte's high nibble (0x80 note off ... 0xE0 pitch bend).
    internal void ProcessMessage(int channel, int command, int data1, int data2)
    {
        switch (command)
        {
            case 0x80:
                NoteOff(channel, data1);
                break;
            case 0x90:
                NoteOn(channel, data1, data2);
                break;
            case 0xB0:
                ControlChange(channel, data1, data2);
                break;
            case 0xC0:
                _channels[channel].ProgramChange(data1);
                break;
            case 0xE0:
                _channels[channel].PitchBend(data1, data2);
                break;
        }
    }

    internal void NoteOn(int channel, int key, int velocity)
    {
        if (velocity == 0)
        {
            NoteOff(channel, key);
            return;
        }

        MidiChannel state = _channels[channel];
        IReadOnlyList<VoiceParameters> found = _soundFont.FindVoices(state.Bank, state.Program, key, velocity);

        foreach (VoiceParameters parameters in found)
        {
            // A new note of an exclusive class (an open hi-hat closed by a closed one) cuts the old one dead.
            if (parameters.ExclusiveClass != 0)
            {
                foreach (Voice voice in _voices)
                {
                    if (!voice.IsFinished && voice.Channel == channel && voice.ExclusiveClass == parameters.ExclusiveClass)
                    {
                        voice.Stop();
                    }
                }
            }
        }

        foreach (VoiceParameters parameters in found)
        {
            FreeVoice().Start(parameters, _soundFont.SampleData, channel, key, velocity, _startOrder++);
        }
    }

    internal void NoteOff(int channel, int key)
    {
        bool sustain = _channels[channel].Sustain;

        foreach (Voice voice in _voices)
        {
            if (!voice.IsFinished && !voice.IsReleased && voice.Channel == channel && voice.Key == key)
            {
                if (sustain)
                {
                    voice.IsHeld = true;
                }
                else
                {
                    voice.Release();
                }
            }
        }
    }

    // Mixes every sounding voice into an interleaved stereo buffer, replacing what it held.
    internal void Render(Span<float> stereo)
    {
        stereo.Clear();

        foreach (Voice voice in _voices)
        {
            voice.Render(stereo, _channels[voice.Channel]);
        }

        for (int i = 0; i < stereo.Length; i++)
        {
            stereo[i] *= MasterGain;
        }
    }

    private void ControlChange(int channel, int controller, int value)
    {
        _channels[channel].ControlChange(controller, value);

        switch (controller)
        {
            case 64 when value < 64:
                ForEachVoice(channel, v => v.IsHeld, v => v.Release());
                break;
            case 120:
                ForEachVoice(channel, _ => true, v => v.Stop());
                break;
            case 123:
                ForEachVoice(channel, v => !v.IsReleased, v => v.Release());
                break;
        }
    }

    private void ForEachVoice(int channel, Func<Voice, bool> match, Action<Voice> action)
    {
        foreach (Voice voice in _voices)
        {
            if (!voice.IsFinished && voice.Channel == channel && match(voice))
            {
                action(voice);
            }
        }
    }

    // A finished voice; else the oldest in release; else the oldest of all.
    private Voice FreeVoice()
    {
        Voice? oldestReleased = null;
        Voice oldest = _voices[0];

        foreach (Voice voice in _voices)
        {
            if (voice.IsFinished)
            {
                return voice;
            }

            if (voice.IsReleased && (oldestReleased is null || voice.StartOrder < oldestReleased.StartOrder))
            {
                oldestReleased = voice;
            }

            if (voice.StartOrder < oldest.StartOrder)
            {
                oldest = voice;
            }
        }

        return oldestReleased ?? oldest;
    }
}

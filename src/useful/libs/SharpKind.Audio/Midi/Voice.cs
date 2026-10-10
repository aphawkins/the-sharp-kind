// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.SoundFonts;

namespace SharpKind.Audio.Midi;

// Plays one SoundFont sample for one note, with linear interpolation, its loop and its volume envelope.
internal sealed class Voice
{
    private const int LoopForever = 1;
    private const int LoopUntilRelease = 3;

    // SoundFonts are voiced for the EMU hardware, which applies 40% of the initial attenuation a font asks for.
    private const double AttenuationScale = 0.4;

    private readonly VolumeEnvelope _envelope = new();

    private ReadOnlyMemory<short> _data;
    private VoiceParameters _parameters;
    private double _position;
    private float _noteGain;

    internal int Channel { get; private set; }

    internal int Key { get; private set; }

    internal int ExclusiveClass => _parameters.ExclusiveClass;

    // When the voice started, to steal the oldest first.
    internal long StartOrder { get; private set; }

    internal bool IsReleased { get; private set; }

    // Note-off arrived while the sustain pedal was down.
    internal bool IsHeld { get; set; }

    internal bool IsFinished => HasEnded || _envelope.IsFinished;

    // The sample ran out, or the voice was stopped.
    private bool HasEnded { get; set; } = true;

    internal void Start(in VoiceParameters parameters, in ReadOnlyMemory<short> data, int channel, int key, int velocity, long startOrder)
    {
        _parameters = parameters;
        _data = data;
        _position = parameters.SampleStart;
        _noteGain = (float)(VolumeEnvelope.CentibelsToGain(parameters.Attenuation * AttenuationScale) * Math.Pow(velocity / 127.0, 2));
        HasEnded = parameters.SampleEnd - parameters.SampleStart < 2;
        _envelope.Start(parameters);

        Channel = channel;
        Key = key;
        StartOrder = startOrder;
        IsReleased = false;
        IsHeld = false;
    }

    internal void Release()
    {
        IsReleased = true;
        IsHeld = false;
        _envelope.Release();
    }

    internal void Stop()
    {
        HasEnded = true;
        _envelope.Stop();
    }

    // Adds this voice into an interleaved stereo buffer, under the channel's current volume, pan and pitch bend.
    internal void Render(Span<float> stereo, MidiChannel channel)
    {
        if (IsFinished)
        {
            return;
        }

        ReadOnlySpan<short> data = _data.Span;
        double step = PitchRatio(channel.BendSemitones);

        float pan = Math.Clamp((_parameters.Pan / 500f) + channel.Pan, -1f, 1f);
        float gain = _noteGain * channel.Gain / 32768f;
        float left = gain * MathF.Cos((pan + 1) * MathF.PI / 4);
        float right = gain * MathF.Sin((pan + 1) * MathF.PI / 4);

        // Release, the only thing that ends a loop, never happens during a render.
        bool looping = IsLooping();
        int loopStart = _parameters.LoopStart;
        int loopEnd = _parameters.LoopEnd;
        int sampleEnd = _parameters.SampleEnd;

        for (int i = 0; i < stereo.Length; i += 2)
        {
            _envelope.Step();
            if (_envelope.IsFinished)
            {
                return;
            }

            int index = (int)_position;
            int next = index + 1;

            if (looping && next >= loopEnd)
            {
                next = loopStart;
            }
            else if (next >= sampleEnd)
            {
                HasEnded = true;
                return;
            }

            float fraction = (float)(_position - index);
            float sample = (data[index] + ((data[next] - data[index]) * fraction)) * (float)_envelope.Gain;

            stereo[i] += sample * left;
            stereo[i + 1] += sample * right;

            _position += step;
            if (looping && _position >= loopEnd)
            {
                _position -= loopEnd - loopStart;
            }
        }
    }

    private bool IsLooping()
        => _parameters.LoopEnd > _parameters.LoopStart
            && (_parameters.SampleModes == LoopForever || (_parameters.SampleModes == LoopUntilRelease && !IsReleased));

    private double PitchRatio(double bendSemitones)
    {
        double semitones = ((Key - _parameters.RootKey) * _parameters.ScaleTuning / 100.0)
            + _parameters.CoarseTune
            + ((_parameters.FineTune + _parameters.PitchCorrection) / 100.0)
            + bendSemitones;

        return Math.Pow(2, semitones / 12) * _parameters.SampleRate / SoftwareSound.SampleRate;
    }
}

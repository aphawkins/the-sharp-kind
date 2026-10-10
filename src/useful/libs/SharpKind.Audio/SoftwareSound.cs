// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Assets;
using SharpKind.Audio.Midi;
using SharpKind.Audio.Wav;

namespace SharpKind.Audio;

// Pure-managed equivalent of SDLSound: decode, mixing, resampling and pitch-shift in C# (.mid through a SoundFont, .wav, .sid), exposing a pull-based Render for a thin SDL output shim to call.
public sealed class SoftwareSound : ISound, IDisposable
{
    // Matches SDLSound's fixed pool of 16 concurrent one-shot voices; dropped (not queued) when the pool is full.
    private const int OneShotVoiceCount = 16;

    // Safety cap on decode length (incl. MIDI release tails), guarding only against a pathological source never going silent.
    private const int MaxDecodeSeconds = 30;

    private readonly Lock _gate = new();
    private readonly IDictionary<string, string> _musicPaths;
    private readonly string? _soundFontPath;
    private readonly Dictionary<string, float[]> _sfxSamples;
    private readonly OneShotVoice[] _oneShotVoices;

    // SID music and effects share one emulated chip, so they are played apart from the samples.
    private readonly SidSound? _sid;

    private IMusicSource? _music;
    private float[] _musicScratch = [];

    private float[] _loopSamples = [];
    private string? _loopName;
    private double _loopPitch = 1.0;
    private double _loopPosition;
    private bool _isDisposed;

    public SoftwareSound(IAssetLocator assetLocator)
    {
        ArgumentNullException.ThrowIfNull(assetLocator);

        _musicPaths = assetLocator.MusicPaths;
        _soundFontPath = assetLocator.SoundFontPaths.Values.FirstOrDefault();

        SidSound sid = new(assetLocator);
        _sid = sid.HasAny ? sid : null;

        // SFX (and the pitch-shiftable loop, reusing this cache) are decoded fully up front, matching SDLSound's predecode: true.
        _sfxSamples = assetLocator.SfxPaths
            .Where(x => !SidSound.IsSidEffect(x.Value))
            .ToDictionary(x => x.Key, x => DecodeFully(x.Value));

        _oneShotVoices = new OneShotVoice[OneShotVoiceCount];
        for (int i = 0; i < OneShotVoiceCount; i++)
        {
            _oneShotVoices[i] = new OneShotVoice();
        }
    }

    // Fixed contract shape shared with the downstream SDL output shim.
    public static int SampleRate { get; } = 44100;

    public static int Channels { get; } = 2;

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void Play(string musicType, bool repeat)
    {
        if (_sid?.HandlesMusic(musicType) == true)
        {
            lock (_gate)
            {
                StopMusicLocked();
                _sid.PlayMusic(musicType, repeat);
            }

            return;
        }

        // Building the stream involves blocking I/O (and, first Play() of a .mid, loading a SoundFont2), so it
        // happens outside _gate to avoid glitching Render() on the audio thread. Only the reference swap is locked.
        IMusicSource newMusic = MusicSources.Open(_musicPaths[musicType], _soundFontPath, repeat);

        lock (_gate)
        {
            StopMusicLocked();
            _sid?.StopMusic();
            _music = newMusic;
        }
    }

    public void Play(string sfxType, float volume, float pan, double pitch)
    {
        // A SID effect has its own volume and pitch, as on the C64.
        if (_sid?.HandlesEffect(sfxType) == true)
        {
            _sid.PlayEffect(sfxType);
            return;
        }

        lock (_gate)
        {
            OneShotVoice? voice = Array.Find(_oneShotVoices, v => !v.Active);
            if (voice is null)
            {
                // Pool exhausted: drop the effect, matching SDLSound's Mix_PlayChannel(-1, ...) behaviour.
                return;
            }

            voice.Samples = _sfxSamples[sfxType];
            voice.Position = 0;
            voice.Pitch = pitch;
            voice.LeftGain = ToLeftGain(pan) * ToTrackGain(volume);
            voice.RightGain = ToRightGain(pan) * ToTrackGain(volume);
            voice.Active = true;
        }
    }

    public void StopMusic()
    {
        lock (_gate)
        {
            StopMusicLocked();
            _sid?.StopMusic();
        }
    }

    public void PlayLoop(string sfxType, double pitch)
    {
        lock (_gate)
        {
            if (_loopName != sfxType)
            {
                _loopSamples = _sfxSamples[sfxType];
                _loopPosition = 0;
                _loopName = sfxType;
            }

            _loopPitch = pitch;
        }
    }

    public void StopLoop()
    {
        lock (_gate)
        {
            _loopName = null;
            _loopSamples = [];
            _loopPosition = 0;
        }
    }

    // Safe to call concurrently with the ISound control methods above; both serialise on the same gate.
    public void Render(in Span<float> buffer)
    {
        lock (_gate)
        {
            buffer.Clear();

            RenderMusic(buffer);
            RenderSid(buffer);
            RenderLoop(buffer);
            RenderOneShots(buffer);

            // Overlapping sources can sum past +/-1; clamp so output is always well-formed PCM, as a hardware mixer would clip.
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = Math.Clamp(buffer[i], -1f, 1f);
            }
        }
    }

    private static float ToTrackGain(float volume) => Math.Clamp(volume, 0f, 1f);

    private static float ToLeftGain(float pan) => Math.Clamp((1f - pan) / 2f, 0f, 1f);

    private static float ToRightGain(float pan) => Math.Clamp((1f + pan) / 2f, 0f, 1f);

    private static float Lerp(float a, float b, float t) => a + ((b - a) * t);

    private static void RenderVoice(OneShotVoice voice, in Span<float> buffer)
    {
        int frames = voice.Samples.Length / Channels;
        if (frames < 2)
        {
            voice.Active = false;
            return;
        }

        for (int i = 0; i < buffer.Length; i += Channels)
        {
            int frame0 = (int)voice.Position;
            if (frame0 >= frames - 1)
            {
                voice.Active = false;
                return;
            }

            int frame1 = frame0 + 1;
            float fraction = (float)(voice.Position - frame0);

            buffer[i] += Lerp(voice.Samples[frame0 * 2], voice.Samples[frame1 * 2], fraction) * voice.LeftGain;
            buffer[i + 1] += Lerp(voice.Samples[(frame0 * 2) + 1], voice.Samples[(frame1 * 2) + 1], fraction) * voice.RightGain;

            voice.Position += voice.Pitch;
        }
    }

    private float[] DecodeFully(string path)
        => MusicSources.HasExtension(path, ".mid")
            ? DecodeMidiFully(path)
            : MusicSources.HasExtension(path, ".wav") ? WavDecoder.Decode(path) : throw MusicSources.UnsupportedFormat(path);

    private float[] DecodeMidiFully(string path)
    {
        Synthesizer synthesizer = new(MusicSources.LoadSoundFont(_soundFontPath));
        MidiSequencer sequencer = new(MidiFile.Read(path), synthesizer, repeat: false);

        int maxSamples = SampleRate * Channels * MaxDecodeSeconds;
        List<float> samples = [];
        float[] chunk = new float[SampleRate * Channels / 10];

        // Keep rendering past the last message until every voice has
        // finished its release tail, not just until the last MIDI event.
        while ((!sequencer.EndOfSequence || synthesizer.ActiveVoiceCount > 0) && samples.Count < maxSamples)
        {
            sequencer.Render(chunk);
            samples.AddRange(chunk);
        }

        return [.. samples];
    }

    private void StopMusicLocked()
    {
        _music?.Dispose();
        _music = null;
    }

    private void RenderMusic(in Span<float> buffer)
    {
        if (_music is null)
        {
            return;
        }

        Span<float> scratch = Scratch(buffer.Length);
        _music.Render(scratch);

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] += scratch[i];
        }
    }

    private Span<float> Scratch(int length)
    {
        if (_musicScratch.Length < length)
        {
            _musicScratch = new float[length];
        }

        return _musicScratch.AsSpan(0, length);
    }

    private void RenderSid(in Span<float> buffer)
    {
        if (_sid is null)
        {
            return;
        }

        Span<float> scratch = Scratch(buffer.Length);
        _sid.Render(scratch);

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] += scratch[i];
        }
    }

    private void RenderLoop(in Span<float> buffer)
    {
        if (_loopName is null)
        {
            return;
        }

        int frames = _loopSamples.Length / Channels;
        if (frames < 2)
        {
            return;
        }

        double pitch = _loopPitch;
        for (int i = 0; i < buffer.Length; i += Channels)
        {
            int frame0 = (int)_loopPosition;
            int frame1 = frame0 + 1 >= frames ? 0 : frame0 + 1;
            float fraction = (float)(_loopPosition - frame0);

            buffer[i] += Lerp(_loopSamples[frame0 * 2], _loopSamples[frame1 * 2], fraction);
            buffer[i + 1] += Lerp(_loopSamples[(frame0 * 2) + 1], _loopSamples[(frame1 * 2) + 1], fraction);

            _loopPosition += pitch;
            while (_loopPosition >= frames)
            {
                _loopPosition -= frames;
            }
        }
    }

    private void RenderOneShots(in Span<float> buffer)
    {
        foreach (OneShotVoice voice in _oneShotVoices)
        {
            if (voice.Active)
            {
                RenderVoice(voice, buffer);
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _music?.Dispose();
                _music = null;
                _sid?.Dispose();
            }

            _isDisposed = true;
        }
    }

    // Reused, mutable slot in the fixed one-shot voice pool; avoids allocating per Play() call.
    private sealed class OneShotVoice
    {
        public float[] Samples { get; set; } = [];

        public double Position { get; set; }

        public double Pitch { get; set; } = 1.0;

        public float LeftGain { get; set; }

        public float RightGain { get; set; }

        public bool Active { get; set; }
    }
}

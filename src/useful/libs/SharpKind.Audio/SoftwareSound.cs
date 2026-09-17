// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MeltySynth;
using NVorbis;
using SharpKind.Assets;

namespace SharpKind.Audio;

// Pure-managed equivalent of SDLSound: decode, mixing, resampling and pitch-shift in C# (MeltySynth .mid, NVorbis .ogg), exposing a pull-based Render for a thin SDL output shim to call.
public sealed class SoftwareSound : ISound, IDisposable
{
    // Matches SDLSound's fixed pool of 16 concurrent one-shot voices; dropped (not queued) when the pool is full.
    private const int OneShotVoiceCount = 16;

    // Safety cap on decode length (incl. MIDI release tails), guarding only against a pathological source never going silent.
    private const int MaxDecodeSeconds = 30;

    private readonly Lock _gate = new();
    private readonly IDictionary<string, string> _musicPaths;
    private readonly IDictionary<string, string> _soundFontPaths;
    private readonly Dictionary<string, float[]> _sfxSamples;
    private readonly OneShotVoice[] _oneShotVoices;

    private SoundFont? _soundFont;
    private IMusicStream? _music;
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
        _soundFontPaths = assetLocator.SoundFontPaths;

        // SFX (and the pitch-shiftable loop, reusing this cache) are decoded fully up front, matching SDLSound's predecode: true.
        _sfxSamples = assetLocator.SfxPaths.ToDictionary(x => x.Key, x => DecodeFully(x.Value));

        _oneShotVoices = new OneShotVoice[OneShotVoiceCount];
        for (int i = 0; i < OneShotVoiceCount; i++)
        {
            _oneShotVoices[i] = new OneShotVoice();
        }
    }

    // Streamed (not predecoded) interleaved stereo float32 audio for the current music track, shared by both backends.
    private interface IMusicStream : IDisposable
    {
        public void Render(Span<float> buffer);
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
        // Building the stream involves blocking I/O (and, first Play() of a .mid, loading a SoundFont2), so it
        // happens outside _gate to avoid glitching Render() on the audio thread. Only the reference swap is locked.
        IMusicStream newMusic = CreateMusicStream(_musicPaths[musicType], repeat);

        lock (_gate)
        {
            StopMusicLocked();
            _music = newMusic;
        }
    }

    public void Play(string sfxType, float volume, float pan, double pitch)
    {
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

    private static bool IsMidi(string path) => string.Equals(Path.GetExtension(path), ".mid", StringComparison.OrdinalIgnoreCase);

    private static bool IsWav(string path) => string.Equals(Path.GetExtension(path), ".wav", StringComparison.OrdinalIgnoreCase);

    private static float[] DecodeOggFully(string path)
    {
        using VorbisReader reader = new(path);

        // No generic sample-rate/channel converter for .ogg; assets must already match the mixer's format.
        Debug.Assert(reader.Channels == Channels, "SoftwareSound assumes .ogg assets are already encoded at the mixer's channel count.");
        Debug.Assert(reader.SampleRate == SampleRate, "SoftwareSound assumes .ogg assets are already encoded at the mixer's sample rate.");

        List<float> samples = [];
        float[] chunk = new float[SampleRate * Channels / 10];
        int read;
        while ((read = reader.ReadSamples(chunk)) > 0)
        {
            samples.AddRange(chunk.AsSpan(0, read));
        }

        return [.. samples];
    }

    // Manual RIFF/WAVE chunk walk (not fixed 44-byte header) since some files carry extra chunks (e.g. LIST/INFO) before 'data'. Only PCM16 (tag 1) and float32 (tag 3) are supported.
    private static float[] DecodeWavFully(string path)
    {
        ReadOnlySpan<byte> span = File.ReadAllBytes(path);

        if (span.Length < 12 || !span[..4].SequenceEqual("RIFF"u8) || !span.Slice(8, 4).SequenceEqual("WAVE"u8))
        {
            throw new SharpKindException($"'{path}' is not a valid WAV file.");
        }

        ushort formatTag = 0;
        ushort channels = 0;
        uint sampleRate = 0;
        ushort bitsPerSample = 0;
        ReadOnlySpan<byte> data = default;

        int offset = 12;
        while (offset + 8 <= span.Length)
        {
            ReadOnlySpan<byte> chunkId = span.Slice(offset, 4);
            uint chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 4, 4));
            int chunkStart = offset + 8;
            int chunkLength = (int)Math.Min(chunkSize, (uint)Math.Max(span.Length - chunkStart, 0));

            if (chunkId.SequenceEqual("fmt "u8))
            {
                ReadOnlySpan<byte> fmt = span.Slice(chunkStart, chunkLength);
                if (fmt.Length < 16)
                {
                    throw new SharpKindException($"'{path}' has a truncated 'fmt ' chunk.");
                }

                formatTag = BinaryPrimitives.ReadUInt16LittleEndian(fmt);
                channels = BinaryPrimitives.ReadUInt16LittleEndian(fmt[2..]);
                sampleRate = BinaryPrimitives.ReadUInt32LittleEndian(fmt[4..]);
                bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(fmt[14..]);
            }
            else if (chunkId.SequenceEqual("data"u8))
            {
                data = span.Slice(chunkStart, chunkLength);
            }

            // Chunks are word-aligned: an odd-sized chunk is followed by a single pad byte.
            offset = chunkStart + chunkLength + ((chunkSize % 2 == 1) ? 1 : 0);
        }

        if (data.IsEmpty)
        {
            throw new SharpKindException($"'{path}' has no 'data' chunk.");
        }

        // No generic sample-rate/channel converter for .wav; assets must already match the mixer's format.
        Debug.Assert(channels == Channels, "SoftwareSound assumes .wav assets are already encoded at the mixer's channel count.");
        Debug.Assert(sampleRate == SampleRate, "SoftwareSound assumes .wav assets are already encoded at the mixer's sample rate.");

        return (formatTag, bitsPerSample) switch
        {
            (3, 32) => MemoryMarshal.Cast<byte, float>(data).ToArray(),
            (1, 16) => DecodePcm16(data),
            _ => throw new SharpKindException($"'{path}' uses an unsupported WAV format (tag {formatTag}, {bitsPerSample}-bit)."),
        };
    }

    private static float[] DecodePcm16(in ReadOnlySpan<byte> data)
    {
        ReadOnlySpan<short> samples = MemoryMarshal.Cast<byte, short>(data);
        float[] result = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            result[i] = samples[i] / 32768f;
        }

        return result;
    }

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
        => IsMidi(path) ? DecodeMidiFully(path) : IsWav(path) ? DecodeWavFully(path) : DecodeOggFully(path);

    private SoundFont GetSoundFont()
    {
        if (_soundFont is null)
        {
            if (_soundFontPaths.Count == 0)
            {
                throw new SharpKindException("No SoundFont is configured in the asset manifest; cannot decode a .mid asset.");
            }

            _soundFont = new SoundFont(_soundFontPaths.Values.First());
        }

        return _soundFont;
    }

    private float[] DecodeMidiFully(string path)
    {
        Synthesizer synthesizer = new(GetSoundFont(), SampleRate);
        MidiFile midiFile = new(path);
        MidiFileSequencer sequencer = new(synthesizer);
        sequencer.Play(midiFile, loop: false);

        int maxSamples = SampleRate * Channels * MaxDecodeSeconds;
        List<float> samples = [];
        float[] chunk = new float[SampleRate * Channels / 10];

        // Keep rendering past the last message until every voice has
        // finished its release tail, not just until the last MIDI event.
        while ((!sequencer.EndOfSequence || synthesizer.ActiveVoiceCount > 0) && samples.Count < maxSamples)
        {
            sequencer.RenderInterleaved(chunk);
            samples.AddRange(chunk);
        }

        return [.. samples];
    }

    private IMusicStream CreateMusicStream(string path, bool repeat)
        => IsMidi(path)
            ? new MidiMusicStream(new MidiFile(path), new Synthesizer(GetSoundFont(), SampleRate), repeat)
            : new OggMusicStream(path, repeat);

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

        if (_musicScratch.Length < buffer.Length)
        {
            _musicScratch = new float[buffer.Length];
        }

        Span<float> scratch = _musicScratch.AsSpan(0, buffer.Length);
        _music.Render(scratch);

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

    // MeltySynth's own sequencer handles looping (via the loop flag) and goes silent once a non-looping track's tails are exhausted.
    private sealed class MidiMusicStream : IMusicStream
    {
        private readonly MidiFileSequencer _sequencer;

        public MidiMusicStream(MidiFile midiFile, Synthesizer synthesizer, bool repeat)
        {
            _sequencer = new(synthesizer);
            _sequencer.Play(midiFile, repeat);
        }

        public void Render(Span<float> buffer) => _sequencer.RenderInterleaved(buffer);

        // MeltySynth's MIDI objects hold no unmanaged/disposable resources.
        public void Dispose()
        {
        }
    }

    // NVorbis has no built-in looping, so end-of-stream seeks back to the start when repeat is requested.
    private sealed class OggMusicStream : IMusicStream
    {
        // Bounds seek-back retries per Render call, guarding against an infinite loop on a degenerate (e.g. zero-length) source.
        private const int MaxSeekRetries = 4;

        private readonly VorbisReader _reader;
        private readonly bool _repeat;

        public OggMusicStream(string path, bool repeat)
        {
            _reader = new(path);
            _repeat = repeat;

            Debug.Assert(
                _reader.Channels == Channels,
                "SoftwareSound assumes .ogg assets are already encoded at the mixer's channel count.");
            Debug.Assert(
                _reader.SampleRate == SampleRate,
                "SoftwareSound assumes .ogg assets are already encoded at the mixer's sample rate.");
        }

        public void Render(Span<float> buffer)
        {
            int filled = 0;
            int retries = 0;

            while (filled < buffer.Length)
            {
                int read = _reader.ReadSamples(buffer[filled..]);
                if (read == 0)
                {
                    if (_repeat && retries++ < MaxSeekRetries)
                    {
                        _reader.SamplePosition = 0;
                        continue;
                    }

                    buffer[filled..].Clear();
                    break;
                }

                filled += read;
            }
        }

        public void Dispose() => _reader.Dispose();
    }
}

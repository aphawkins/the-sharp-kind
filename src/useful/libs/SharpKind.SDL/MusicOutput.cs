// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;
using SharpKind.Audio;
using static SDL.SDL3;

namespace SharpKind.SDL;

// Plays music rendered by SharpKind.Audio (a .mid through a SoundFont, or a SID song) beside SDL3_mixer's tracks, which
// cannot take a stream of our own: a raw audio-device stream of its own, filled on SDL's audio thread from the current
// IMusicSource, as SoftwareSoundOutput does for SoftwareSound. SDL mixes the two devices' output.
internal sealed unsafe class MusicOutput : IDisposable
{
    private const int Channels = 2;
    private const int SampleRate = 44100;

    private readonly Lock _gate = new();
    private readonly nint _stream;
    private GCHandle _selfHandle;
    private IMusicSource? _music;
    private float[] _scratch = [];
    private bool _isDisposed;

    internal MusicOutput()
    {
        SDLGuard.Execute(() => SDL_Init(SDL_InitFlags.SDL_INIT_AUDIO));

        _selfHandle = GCHandle.Alloc(this);

        _stream = SDLGuard.Execute(() => OpenDeviceStream(GCHandle.ToIntPtr(_selfHandle)));
        SDLGuard.Execute(() => ResumeStreamDevice(_stream));
    }

    ~MusicOutput()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    // The caller opens the music outside the gate, since that reads files; only the swap is locked.
    internal void Play(IMusicSource music) => Swap(music)?.Dispose();

    internal void Stop() => Swap(null)?.Dispose();

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void FillCallback(nint userData, SDL_AudioStream* stream, int additionalAmount, int totalAmount)
    {
        if (additionalAmount > 0 && GCHandle.FromIntPtr(userData).Target is MusicOutput output)
        {
            output.Fill(stream, additionalAmount);
        }
    }

    private static nint OpenDeviceStream(in nint userData)
    {
        SDL_AudioSpec spec = default;
        spec.format = SDL_AUDIO_F32;
        spec.channels = Channels;
        spec.freq = SampleRate;
        return (nint)SDL_OpenAudioDeviceStream(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &spec, &FillCallback, userData);
    }

    private static bool ResumeStreamDevice(in nint stream) => SDL_ResumeAudioStreamDevice((SDL_AudioStream*)stream);

    private static void DestroyStream(in nint stream) => SDL_DestroyAudioStream((SDL_AudioStream*)stream);

    private void Fill(SDL_AudioStream* stream, int additionalAmount)
    {
        int sampleCount = additionalAmount / sizeof(float);

        if (_scratch.Length < sampleCount)
        {
            _scratch = new float[sampleCount];
        }

        Span<float> buffer = _scratch.AsSpan(0, sampleCount);

        lock (_gate)
        {
            if (_music is null)
            {
                buffer.Clear();
            }
            else
            {
                _music.Render(buffer);
            }
        }

        fixed (float* ptr = buffer)
        {
            _ = SDL_PutAudioStreamData(stream, (nint)ptr, additionalAmount);
        }
    }

    private IMusicSource? Swap(IMusicSource? music)
    {
        lock (_gate)
        {
            IMusicSource? old = _music;
            _music = music;
            return old;
        }
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (disposing)
        {
            Stop();
        }

        DestroyStream(_stream);

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }
}

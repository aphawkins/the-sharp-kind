// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;
using SharpKind.Audio;
using static SDL.SDL3;

namespace SharpKind.SDL;

// Thin SDL3 raw-audio-device shim for SoftwareSound, mirroring how SoftwareScreenUpdate just
// blits an already-rendered FastBitmap: decode/mix already happened in SoftwareSound.Render, so
// this only owns the SDL_AudioStream/device pair and pulls data on SDL's callback. Built on
// SDL3's core SDL_OpenAudioDeviceStream, not SDL3_mixer, which this (live) path doesn't use.
public sealed unsafe class SoftwareSoundOutput : IDisposable
{
    private readonly SoftwareSound _sound;
    private readonly nint _stream;
    private GCHandle _selfHandle;
    private float[] _scratch = [];
    private bool _isDisposed;

    public SoftwareSoundOutput(SoftwareSound sound)
    {
        ArgumentNullException.ThrowIfNull(sound);

        _sound = sound;

        SDLGuard.Execute(() => SDL_Init(SDL_InitFlags.SDL_INIT_AUDIO));

        // A stable handle so the static [UnmanagedCallersOnly] fill callback can find its way back via userdata.
        _selfHandle = GCHandle.Alloc(this);

        _stream = SDLGuard.Execute(() => OpenDeviceStream(
            SDL_AUDIO_F32,
            SoftwareSound.Channels,
            SoftwareSound.SampleRate,
            GCHandle.ToIntPtr(_selfHandle)));

        SDLGuard.Execute(() => ResumeStreamDevice(_stream));
    }

    // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~SoftwareSoundOutput()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    // Called on SDL's own audio thread. No locking (Render is documented safe from any thread) and
    // no allocation on the steady-state path (the scratch buffer only grows).
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void FillCallback(nint userData, SDL_AudioStream* stream, int additionalAmount, int totalAmount)
    {
        if (additionalAmount > 0 && GCHandle.FromIntPtr(userData).Target is SoftwareSoundOutput output)
        {
            output.Fill(stream, additionalAmount);
        }
    }

    // Built locally from primitive components, not a struct/pointer parameter, to stay safe inside an SDLGuard.Execute lambda.
    private static nint OpenDeviceStream(SDL_AudioFormat format, int channels, int freq, in nint userData)
    {
        SDL_AudioSpec spec = default;
        spec.format = format;
        spec.channels = channels;
        spec.freq = freq;
        return (nint)SDL_OpenAudioDeviceStream(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &spec, &FillCallback, userData);
    }

    // SDL3 opens new audio devices in a paused state; this resumes it so the
    // fill callback above actually starts being invoked.
    private static bool ResumeStreamDevice(in nint stream) => SDL_ResumeAudioStreamDevice((SDL_AudioStream*)stream);

    // Destroying the stream also closes the audio device it implicitly opened; no separate handle to close.
    private static void DestroyStream(in nint stream) => SDL_DestroyAudioStream((SDL_AudioStream*)stream);

    private void Fill(SDL_AudioStream* stream, int additionalAmount)
    {
        int sampleCount = additionalAmount / sizeof(float);
        if (_scratch.Length < sampleCount)
        {
            _scratch = new float[sampleCount];
        }

        Span<float> buffer = _scratch.AsSpan(0, sampleCount);
        _sound.Render(buffer);

        fixed (float* ptr = buffer)
        {
            _ = SDL_PutAudioStreamData(stream, (nint)ptr, additionalAmount);
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (disposing)
            {
                // dispose managed state (managed objects)
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            DestroyStream(_stream);

            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }
        }
    }
}

// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using MeltySynth;
using SDL;
using SharpKind.Assets;
using SharpKind.Audio;
using static SDL.SDL3;
using static SDL.SDL3_mixer;

namespace SharpKind.SDL;

// Hardware-accelerated counterpart to SoftwareSound: decode/mix/pitch/pan are all done by
// SDL3_mixer's MIX_Track API. Unlike SDL2_mixer, SDL3_mixer exposes per-track pitch and panning
// natively, so no resample-via-effect-callback trick is needed.
//
// .mid music is the one asset SDL3_mixer's bundled decoder can't open (its Timidity backend needs
// a GUS patch set this project doesn't ship), so .mid tracks are predecoded with the same
// MeltySynth + SoundFont2 path SoftwareSound uses and handed to the mixer as raw PCM.
public sealed unsafe class SDLSound : ISound, IDisposable
{
    // Matches SoftwareSound's fixed pool of 16 concurrent one-shot voices; a
    // one-shot is dropped (not queued or errored) when every track in the
    // pool is still playing.
    private const int OneShotVoiceCount = 16;

    // Format MIX_LoadRawAudio is told the predecoded .mid PCM is in; matches
    // the mixer device's own format so no further conversion is needed.
    private const int SampleRate = 44100;
    private const int Channels = 2;

    // Guards only against a pathological source never going silent; must stay well above any real track's length or looping music truncates audibly.
    private const int MaxDecodeSeconds = 300;

    private readonly Dictionary<string, nint> _music;
    private readonly Dictionary<string, nint> _sfx;
    private readonly nint[] _oneShotTracks;
    private readonly nint _loopTrack;
    private readonly nint _musicTrack;
    private readonly nint _mixer;
    private string? _loopName;
    private bool _isDisposed;

    public SDLSound(IAssetLocator assetLocator)
    {
        ArgumentNullException.ThrowIfNull(assetLocator);

        SDLGuard.Execute(() => SDL_Init(SDL_InitFlags.SDL_INIT_AUDIO));
        SDLGuard.Execute(() => MIX_Init());

        _mixer = SDLGuard.Execute(() => (nint)MIX_CreateMixerDevice(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, null));

        string? soundFontPath = assetLocator.SoundFontPaths.Count > 0 ? assetLocator.SoundFontPaths.Values.First() : null;

        _music = assetLocator.MusicPaths.ToDictionary(
            x => x.Key,
            x => LoadMusicAudio(x.Value, soundFontPath));

        _sfx = assetLocator.SfxPaths.ToDictionary(
            x => x.Key,
            x => SDLGuard.Execute(() => (nint)MIX_LoadAudio((MIX_Mixer*)_mixer, x.Value, predecode: true)));

        _musicTrack = SDLGuard.Execute(() => (nint)MIX_CreateTrack((MIX_Mixer*)_mixer));
        _loopTrack = SDLGuard.Execute(() => (nint)MIX_CreateTrack((MIX_Mixer*)_mixer));

        _oneShotTracks = new nint[OneShotVoiceCount];
        for (int i = 0; i < OneShotVoiceCount; i++)
        {
            _oneShotTracks[i] = SDLGuard.Execute(() => (nint)MIX_CreateTrack((MIX_Mixer*)_mixer));
        }
    }

    // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~SDLSound()
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

    public void Play(string musicType, bool repeat)
    {
        MIX_Track* track = (MIX_Track*)_musicTrack;
        SDLGuard.Execute(() => MIX_SetTrackAudio(track, (MIX_Audio*)_music[musicType]));
        SDLGuard.Execute(() => MIX_PlayTrack(track, 0));
        SDLGuard.Execute(() => MIX_SetTrackLoops(track, repeat ? -1 : 0));
    }

    public void Play(string sfxType, float volume, float pan, double pitch)
    {
        nint voicePtr = Array.Find(_oneShotTracks, t => !MIX_TrackPlaying((MIX_Track*)t));
        if (voicePtr == nint.Zero)
        {
            // Pool exhausted: drop the effect, matching SoftwareSound's
            // any-free-voice behaviour.
            return;
        }

        MIX_Track* track = (MIX_Track*)voicePtr;
        SDLGuard.Execute(() => MIX_SetTrackAudio(track, (MIX_Audio*)_sfx[sfxType]));
        SDLGuard.Execute(() => MIX_SetTrackGain(track, volume));
        SDLGuard.Execute(() => MIX_SetTrackFrequencyRatio(track, (float)pitch));
        SDLGuard.Execute(() =>
        {
            MIX_StereoGains gains = new() { left = ToLeftGain(pan), right = ToRightGain(pan) };
            return MIX_SetTrackStereo(track, &gains);
        });
        SDLGuard.Execute(() => MIX_PlayTrack(track, 0));
    }

    public void StopMusic() => SDLGuard.Execute(() => MIX_StopTrack((MIX_Track*)_musicTrack, 0));

    public void PlayLoop(string sfxType, double pitch)
    {
        MIX_Track* track = (MIX_Track*)_loopTrack;

        if (_loopName != sfxType)
        {
            SDLGuard.Execute(() => MIX_SetTrackAudio(track, (MIX_Audio*)_sfx[sfxType]));
            SDLGuard.Execute(() => MIX_PlayTrack(track, 0));
            SDLGuard.Execute(() => MIX_SetTrackLoops(track, -1));
            _loopName = sfxType;
        }

        SDLGuard.Execute(() => MIX_SetTrackFrequencyRatio(track, (float)pitch));
    }

    public void StopLoop()
    {
        if (_loopName is null)
        {
            return;
        }

        SDLGuard.Execute(() => MIX_StopTrack((MIX_Track*)_loopTrack, 0));
        _loopName = null;
    }

    private static float ToLeftGain(float pan) => Math.Clamp((1f - pan) / 2f, 0f, 1f);

    private static float ToRightGain(float pan) => Math.Clamp((1f + pan) / 2f, 0f, 1f);

    private static bool IsMidi(string path) => string.Equals(Path.GetExtension(path), ".mid", StringComparison.OrdinalIgnoreCase);

    private static float[] DecodeMidiFully(string path, string soundFontPath)
    {
        SoundFont soundFont = new(soundFontPath);
        Synthesizer synthesizer = new(soundFont, SampleRate);
        MidiFile midiFile = new(path);
        MidiFileSequencer sequencer = new(synthesizer);
        sequencer.Play(midiFile, loop: false);

        const int maxSamples = SampleRate * Channels * MaxDecodeSeconds;
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

    private nint LoadMusicAudio(string path, string? soundFontPath)
    {
        if (!IsMidi(path))
        {
            return SDLGuard.Execute(() => (nint)MIX_LoadAudio((MIX_Mixer*)_mixer, path, predecode: false));
        }

        if (soundFontPath is null)
        {
            throw new SharpKindException("No SoundFont is configured in the asset manifest; cannot decode a .mid asset.");
        }

        float[] samples = DecodeMidiFully(path, soundFontPath);

        return SDLGuard.Execute(() =>
        {
            SDL_AudioSpec spec = default;
            spec.format = SDL_AUDIO_F32;
            spec.channels = Channels;
            spec.freq = SampleRate;

            fixed (float* ptr = samples)
            {
                return (nint)MIX_LoadRawAudio((MIX_Mixer*)_mixer, (nint)ptr, (nuint)(samples.Length * sizeof(float)), &spec);
            }
        });
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
            foreach (nint track in _oneShotTracks)
            {
                MIX_DestroyTrack((MIX_Track*)track);
            }

            MIX_DestroyTrack((MIX_Track*)_loopTrack);
            MIX_DestroyTrack((MIX_Track*)_musicTrack);

            foreach (nint music in _music.Values)
            {
                MIX_DestroyAudio((MIX_Audio*)music);
            }

            foreach (nint sfx in _sfx.Values)
            {
                MIX_DestroyAudio((MIX_Audio*)sfx);
            }

            MIX_DestroyMixer((MIX_Mixer*)_mixer);
            MIX_Quit();
        }
    }
}

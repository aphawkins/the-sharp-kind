// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

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
// .mid music and SID music and effects are the assets SDL3_mixer can't open (its Timidity backend
// needs a GUS patch set this project doesn't ship), so they are rendered by the same SharpKind.Audio
// code SoftwareSound uses and streamed on MusicOutputs: .mid tracks on one, and the SidSound, whose
// music and effects share one emulated chip, on another for the whole session.
public sealed unsafe class SDLSound : ISound, IDisposable
{
    // Matches SoftwareSound's fixed pool of 16 concurrent one-shot voices; a
    // one-shot is dropped (not queued or errored) when every track in the
    // pool is still playing.
    private const int OneShotVoiceCount = 16;

    private readonly Dictionary<string, nint> _music;
    private readonly Dictionary<string, string> _streamedPaths;
    private readonly string? _soundFontPath;
    private readonly MusicOutput? _streamed;
    private readonly SidSound? _sid;
    private readonly MusicOutput? _sidOutput;
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

        _soundFontPath = assetLocator.SoundFontPaths.Values.FirstOrDefault();

        SidSound sid = new(assetLocator);
        if (sid.HasAny)
        {
            _sid = sid;
            _sidOutput = new MusicOutput();
            _sidOutput.Play(sid);
        }
        else
        {
            sid.Dispose();
        }

        _streamedPaths = assetLocator.MusicPaths.Where(x => IsMidi(x.Value)).ToDictionary(x => x.Key, x => x.Value);
        _streamed = _streamedPaths.Count > 0 ? new MusicOutput() : null;

        _music = assetLocator.MusicPaths
            .Where(x => !_streamedPaths.ContainsKey(x.Key) && !MusicPath.IsSid(x.Value))
            .ToDictionary(
                x => x.Key,
                x => SDLGuard.Execute(() => (nint)MIX_LoadAudio((MIX_Mixer*)_mixer, x.Value, predecode: false)));

        _sfx = assetLocator.SfxPaths.Where(x => !SidSound.IsSidEffect(x.Value)).ToDictionary(
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
        if (_sid?.HandlesMusic(musicType) == true)
        {
            SDLGuard.Execute(() => MIX_StopTrack((MIX_Track*)_musicTrack, 0));
            _streamed?.Stop();
            _sid.PlayMusic(musicType, repeat);
            return;
        }

        _sid?.StopMusic();

        if (_streamedPaths.TryGetValue(musicType, out string? path))
        {
            IMusicSource music = MusicSources.Open(path, _soundFontPath, repeat);
            SDLGuard.Execute(() => MIX_StopTrack((MIX_Track*)_musicTrack, 0));
            _streamed!.Play(music);
            return;
        }

        _streamed?.Stop();

        MIX_Track* track = (MIX_Track*)_musicTrack;
        SDLGuard.Execute(() => MIX_SetTrackAudio(track, (MIX_Audio*)_music[musicType]));
        SDLGuard.Execute(() => MIX_PlayTrack(track, 0));
        SDLGuard.Execute(() => MIX_SetTrackLoops(track, repeat ? -1 : 0));
    }

    public void Play(string sfxType, float volume, float pan, double pitch)
    {
        // A SID effect has its own volume and pitch, as on the C64.
        if (_sid?.HandlesEffect(sfxType) == true)
        {
            _sid.PlayEffect(sfxType);
            return;
        }

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

    public void StopMusic()
    {
        _streamed?.Stop();
        _sid?.StopMusic();
        SDLGuard.Execute(() => MIX_StopTrack((MIX_Track*)_musicTrack, 0));
    }

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

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (disposing)
            {
                _streamed?.Dispose();
                _sidOutput?.Dispose();
                _sid?.Dispose();
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

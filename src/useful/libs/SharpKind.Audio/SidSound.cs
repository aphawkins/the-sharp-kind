// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Assets;
using SharpKind.Audio.Sid;
using SharpKind.Emulation.Sid;

namespace SharpKind.Audio;

/// <summary>
/// The SID music and sound effects of an asset manifest, played on one emulated SID chip as on the C64: a song takes the
/// chip, and effects play only while no song does (C64 Elite's default). Both backends render it.
/// </summary>
public sealed class SidSound : IMusicSource
{
    private const string EffectExtension = ".sidfx";

    private readonly Lock _gate = new();
    private readonly SidMachine _machine = new();
    private readonly SidEffectEngine _engine;
    private readonly Dictionary<string, IReadOnlyList<SidEffectStep>> _effects = [];
    private readonly Dictionary<string, (PsidFile Tune, int? Song)> _music = [];
    private SidSongPlayer? _player;

    /// <summary>Initializes a new instance of the <see cref="SidSound"/> class.</summary>
    /// <param name="assetLocator">The manifest: its <c>.sid</c> music and its <c>.sidfx</c> effects are played here.</param>
    /// <exception cref="SharpKindException">An effect names a sound its file does not have, or a file cannot be read.</exception>
    public SidSound(IAssetLocator assetLocator)
    {
        ArgumentNullException.ThrowIfNull(assetLocator);

        _engine = new(_machine.Chip.Write);
        _machine.IdleFrame = _engine.Tick;

        Dictionary<string, SidEffectFile> effectFiles = [];
        foreach ((string key, string path) in assetLocator.SfxPaths.Where(x => IsSidEffect(x.Value)))
        {
            if (!effectFiles.TryGetValue(path, out SidEffectFile? file))
            {
                file = SidEffectFile.Read(path);
                effectFiles[path] = file;
            }

            _effects[key] = file.Sounds.TryGetValue(key, out IReadOnlyList<SidEffectStep>? sound)
                ? sound
                : throw new SharpKindException($"'{path}' has no sound '{key}'.");
        }

        Dictionary<string, PsidFile> tunes = [];
        foreach ((string key, string path) in assetLocator.MusicPaths.Where(x => MusicPath.IsSid(x.Value)))
        {
            (string file, int? song) = MusicPath.Parse(path);
            if (!tunes.TryGetValue(file, out PsidFile? tune))
            {
                tune = PsidFile.Read(File.ReadAllBytes(file));
                tunes[file] = tune;
            }

            _music[key] = (tune, song);
        }
    }

    /// <summary>Gets a value indicating whether the manifest has anything for this class to play.</summary>
    public bool HasAny => _effects.Count > 0 || _music.Count > 0;

    /// <summary>Whether a sound-effect file is a SID effect file.</summary>
    /// <param name="path">The file.</param>
    /// <returns>Whether it is a <c>.sidfx</c>.</returns>
    public static bool IsSidEffect(string path) => MusicSources.HasExtension(path, EffectExtension);

    /// <summary>Whether a sound effect is played here.</summary>
    /// <param name="key">The manifest key.</param>
    /// <returns>Whether it is.</returns>
    public bool HandlesEffect(string key) => _effects.ContainsKey(key);

    /// <summary>Whether a music track is played here.</summary>
    /// <param name="key">The manifest key.</param>
    /// <returns>Whether it is.</returns>
    public bool HandlesMusic(string key) => _music.ContainsKey(key);

    /// <summary>Starts a sound effect. It is not heard while a song plays.</summary>
    /// <param name="key">The manifest key.</param>
    public void PlayEffect(string key)
    {
        lock (_gate)
        {
            _engine.Trigger(_effects[key]);
        }
    }

    /// <summary>Starts a song, in place of any song or effect playing.</summary>
    /// <param name="key">The manifest key.</param>
    /// <param name="repeat">Whether to start the song again when it ends.</param>
    public void PlayMusic(string key, bool repeat)
    {
        (PsidFile tune, int? song) = _music[key];

        lock (_gate)
        {
            _engine.Flush();
            _player = new(_machine, tune, song, repeat);
        }
    }

    /// <summary>Stops the song, if one plays, as the C64's <c>stopat</c> does: effects end and the chip is silenced.</summary>
    public void StopMusic()
    {
        lock (_gate)
        {
            if (_player is not null)
            {
                StopPlayer();
                _machine.StopMusic();
            }
        }
    }

    /// <inheritdoc/>
    public void Render(Span<float> buffer)
    {
        lock (_gate)
        {
            if (_player is null)
            {
                _machine.Render(buffer);
                return;
            }

            _player.Render(buffer);

            // The player has stopped the machine; effects take over from the next frame.
            if (_player.Finished)
            {
                StopPlayer();
            }
        }
    }

    /// <summary>Does nothing: the emulated chip holds no unmanaged resources.</summary>
    public void Dispose()
    {
    }

    private void StopPlayer()
    {
        _player = null;
        _engine.Flush();
    }
}

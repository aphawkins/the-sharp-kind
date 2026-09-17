// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Microsoft.Extensions.Logging;

namespace SharpKind.Audio;

public sealed class AudioController
{
    private readonly IDictionary<string, SfxSample> _sfx;
    private readonly SfxSample[] _samples;
    private readonly ISound _sound;
    private readonly ILogger? _logger;

    public AudioController(ISound sound, IDictionary<string, SfxSample> sfx, AudioOptions options)
        : this(sound, sfx, options, logger: null)
    {
    }

    public AudioController(ISound sound, IDictionary<string, SfxSample> sfx, AudioOptions options, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(sfx);
        ArgumentNullException.ThrowIfNull(options);

        _sound = sound;
        _sfx = sfx;
        _logger = logger;
        MusicOn = options.MusicOn;
        EffectsOn = options.EffectsOn;

        // Two effect names may share one SfxSample (shared cooldown, matching one hardware buffer), so ticking runs over the distinct samples.
        _samples = [.. new HashSet<SfxSample>(sfx.Values)];
    }

    // Settable so a settings screen can toggle sound mid-game.
    public bool EffectsOn { get; set; }

    public bool MusicOn { get; set; }

    public void PlayEffect(string effectType) => PlayEffect(effectType, volume: null, pitch: 1.0);

    /// <summary>
    /// Play an effect, using the sample's static volume/pan unless overridden.
    /// </summary>
    /// <param name="effectType">The effect's key in the sample dictionary passed to the constructor.</param>
    /// <param name="volume">Per-play volume override (0 silent - 1 full), or null to use the sample's static volume.</param>
    /// <param name="pitch">Per-play pitch multiplier (1.0 = recorded rate).</param>
    public void PlayEffect(string effectType, float? volume, double pitch)
    {
        if (!EffectsOn)
        {
            return;
        }

        // Missing sample is a content gap, not a fatal error - log and play nothing.
        if (!_sfx.TryGetValue(effectType, out SfxSample? sample))
        {
            if (_logger is not null)
            {
                LogMessages.MissingSfxSample(_logger, effectType);
            }

            return;
        }

        if (sample.HasTimeRemaining)
        {
            return;
        }

        sample.ResetTime();
        _sound.Play(effectType, volume ?? sample.Volume, sample.Pan, pitch);
    }

    public void PlayMusic(string musicType, bool loop)
    {
        if (!MusicOn)
        {
            return;
        }

        _sound.Play(musicType, loop);
    }

    // Unconditional: also how the settings screen silences music already playing when switched off.
    public void StopMusic() => _sound.StopMusic();

    /// <summary>
    /// Ages every playing sample by one whole tick of the caller's clock.
    /// </summary>
    /// <remarks>
    /// This overload is what keeps Stunt Car Racer's pacing exactly as it
    /// was: it counts sample lifetimes in its own updates and says nothing
    /// about time, so it calls this one and nothing changes.
    /// </remarks>
    public void UpdateSound() => UpdateSound(1f);

    /// <summary>
    /// Ages every playing sample by a fraction of a tick, for a caller that
    /// updates faster than the rate its runtimes were written against.
    /// </summary>
    public void UpdateSound(float ticks)
    {
        foreach (SfxSample sample in _samples)
        {
            sample.ReduceTimeRemaining(ticks);
        }
    }
}

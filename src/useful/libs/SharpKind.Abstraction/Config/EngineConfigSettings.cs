// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text.Json.Serialization;
using SharpKind.Abstraction.Renditions;

namespace SharpKind.Abstraction.Config;

/// <summary>
/// Settings shared by every game, stored under the config file's <c>engine</c>
/// element. Graphics and sound have a group each; what's left at the top sits
/// across both. Game-specific settings live alongside under <c>game</c>; see
/// <see cref="ConfigSettings{TGameSettings}"/>.
/// </summary>
public sealed class EngineConfigSettings
{
    // Past this the window is larger than any display the game could be
    // shown on, so it is a typo rather than an intention.
    private const int MaxWindowScale = 4;

    // Past these the projection is a fish-eye or a telescope rather than a
    // view out of a cockpit.
    private const int MinFieldOfView = 30;
    private const int MaxFieldOfView = 120;

    // Software (default) or Hardware (SDL-accelerated). Picks the mixer as well as the rasteriser, hence not under Graphics.
    public Backend Backend { get; set; } = Backend.Software;

    // Which attached controller is flying, by the name its driver reports. Empty means unchosen:
    // the first device to arrive is used, including one not attached, so a stick left at home
    // doesn't leave them with none at all. The keyboard is always live and never named here.
    public string ActiveController { get; set; } = string.Empty;

    public GraphicsConfigSettings Graphics { get; set; } = new();

    public SoundConfigSettings Sound { get; set; } = new();

    public LoggingConfigSettings Logging { get; set; } = new();

    // Picks the asset set (music/effects as well as artwork) and, with it, the render resolution
    // and scale. One of the three names in RenditionNames; whether the game ships that one is
    // settled when it is looked for. A string, not the enum, so an unreadable value costs that
    // value alone rather than failing the whole bind. See docs/asset-structure.md.
    public string Rendition { get; set; } = RenditionNames.SixteenBit;

    // Which rendition a repair falls back to. A game shipping no 16-bit art says otherwise, or a
    // repair would leave it unable to start. A fact about the build, so not serialised.
    [JsonIgnore]
    public string FallbackRendition { get; set; } = RenditionNames.SixteenBit;

    // What this setting was called before renditions existed. The binder fills it, Repair folds it
    // into Rendition, and JsonIgnore keeps it from being written back, so a file upgrades itself on save.
    [JsonIgnore]
    public string? Tier { get; set; }

    // How many window pixels each rendered pixel occupies. The game always renders at the tier's
    // native resolution and magnifies only at presentation, so scale 2 doubles pixels, not detail.
    // Integer only. Null (unchosen, not the same as 1) is resolved by each app before the window is made.
    public int? WindowScale { get; set; }

    // Vertical field of view in degrees. Widening it pulls the focal length in, fitting more on
    // screen at smaller size. Null keeps the original's own projection (2*atan(0.5)) exactly.
    public int? FieldOfView { get; set; }

    /// <summary>
    /// Replaces any engine value that cannot be honoured with its default, in
    /// place. Every game validates the same way through this, rather than each
    /// one repeating (or, as Stunt Car Racer used to, skipping) the checks.
    /// </summary>
    /// <returns><see langword="true"/> if anything had to be replaced.</returns>
    public bool Repair()
    {
        bool repaired = false;

        if (!Enum.IsDefined(Backend))
        {
            Backend = Backend.Software;
            repaired = true;
        }

        // The two renditions were once an enum, spelled "8Bit"/"16Bit" in the file; old files still
        // exist. Only applies where the new setting was never written.
        if (!string.IsNullOrWhiteSpace(Tier))
        {
            if (string.Equals(Rendition, RenditionNames.SixteenBit, StringComparison.Ordinal))
            {
                Rendition = Tier;
            }

            Tier = null;
            repaired = true;
        }

        // ActiveController is not checked against what is attached - it's a wish about what may be plugged in later.
        // A rendition is not: there are three, so a legacy spelling folds onto its current name and
        // anything else goes back to the fallback.
        if (!RenditionNames.IsCurrentName(Rendition))
        {
            Rendition = RenditionNames.TryParse(Rendition, out Rendition parsed)
                ? RenditionNames.Of(parsed)
                : FallbackRendition;
            repaired = true;
        }

        // A scale outside what any display could show is a typo, so it goes
        // back to unchosen and the app picks for the commander.
        if (WindowScale is < 1 or > MaxWindowScale)
        {
            WindowScale = null;
            repaired = true;
        }

        // Likewise a field of view no cockpit could have: back to unchosen,
        // and the original's projection applies.
        if (FieldOfView is < MinFieldOfView or > MaxFieldOfView)
        {
            FieldOfView = null;
            repaired = true;
        }

        // Captured rather than chained with || so a short-circuit on one
        // group's result can't skip repairing another.
        bool graphicsRepaired = Graphics.Repair();
        bool loggingRepaired = Logging.Repair();

        return graphicsRepaired || loggingRepaired || repaired;
    }
}

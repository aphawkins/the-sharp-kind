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

    // Which IAbstraction runs the game: Software (default) or Hardware
    // (SDL-accelerated). It picks the mixer as well as the rasteriser, which
    // is why it sits here rather than under Graphics.
    public Backend Backend { get; set; } = Backend.Software;

    // Which attached controller is the one being flown, by the name its
    // driver reports. More than one can be plugged in and only one of them
    // can have the ship.
    //
    // Empty means the commander has not chosen, and the first device to
    // arrive is used - and so is a name that is not attached, because a
    // stick left at home should not leave them with no stick at all. The
    // keyboard is always live and is never named here.
    public string ActiveController { get; set; } = string.Empty;

    public GraphicsConfigSettings Graphics { get; set; } = new();

    public SoundConfigSettings Sound { get; set; } = new();

    public LoggingConfigSettings Logging { get; set; } = new();

    // Which rendition the game draws itself as: picks the asset set and,
    // with it, the render resolution and scale. The asset set covers music
    // and effects as well as the artwork, so this is not graphics-only
    // either.
    //
    // One of the three names in RenditionNames and nothing else - the set is
    // the engine's, so a config file, a folder name and a settings screen
    // cannot disagree about what exists. Whether the game actually ships the
    // one named is settled when it is looked for. See docs/asset-structure.md.
    //
    // Held as a string rather than the enum so one unreadable value costs
    // that value alone: an enum the binder cannot parse fails the whole bind,
    // and the player loses every other setting in the file with it.
    public string Rendition { get; set; } = RenditionNames.SixteenBit;

    // Which rendition a repair falls back to. 16-bit unless a game says
    // otherwise, and a game that ships no 16-bit art has to say otherwise:
    // repairing to a rendition the game does not have would leave it unable
    // to start, which is a worse answer than the bad value it replaced.
    //
    // Not serialised. It is a fact about the build, not a setting, and a
    // player cannot usefully choose it.
    [JsonIgnore]
    public string FallbackRendition { get; set; } = RenditionNames.SixteenBit;

    // What this setting was called before renditions existed, read so a file
    // written by an older build keeps the commander's choice. The binder fills
    // it, Repair folds it into Rendition, and JsonIgnore keeps it from ever
    // being written back - so a file upgrades itself the first time it is
    // saved and the old key does not linger.
    [JsonIgnore]
    public string? Tier { get; set; }

    // How many window pixels each rendered pixel occupies. Independent of
    // Tier: the game always renders at the tier's native resolution and is
    // magnified only at presentation, so scale 2 fills a window twice the
    // size with the same pixels doubled rather than with more detail.
    // Integer only - a fractional scale cannot double pixels evenly.
    //
    // Null means the commander has never chosen one, which is not the same as
    // choosing 1: what an unchosen scale should be is the app's to say, and
    // Elite's answer depends on the rendition. Each app resolves it before
    // the window is made.
    public int? WindowScale { get; set; }

    // The vertical field of view in degrees: how much of the universe the
    // viewport shows. Widening it pulls the focal length in, so more fits on
    // screen and everything in it is smaller.
    //
    // Null means the commander has never chosen one and the original's own
    // projection applies, which is 2*atan(0.5) - a focal length of one screen
    // height. Kept null rather than written out as a number so the classic
    // view stays exactly the classic view.
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

        // The two renditions the game shipped with were once an enum, spelled
        // "8Bit" and "16Bit" in the file. Those files are still out there, so
        // they are read as the names those renditions now go by.
        // Only where the new setting was never written, so a file holding
        // both - which only a hand-edit produces - keeps the new one.
        if (!string.IsNullOrWhiteSpace(Tier))
        {
            if (string.Equals(Rendition, RenditionNames.SixteenBit, StringComparison.Ordinal))
            {
                Rendition = Tier;
            }

            Tier = null;
            repaired = true;
        }

        // ActiveController is not checked against what is attached: it is a
        // wish about whatever may be plugged in later, and a stick that is
        // not here today may well be tomorrow. Blanking it would lose the
        // commander's choice every time they unplugged it.
        //
        // A rendition is not like that. There are three, the engine says which,
        // and a file naming anything else is naming something that cannot
        // exist - so a legacy spelling is folded onto the name it now goes by,
        // and anything else goes back to the default.
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

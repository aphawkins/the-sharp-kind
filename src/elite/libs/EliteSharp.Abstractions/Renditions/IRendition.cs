// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharp.Abstractions.Views.Planets;
using EliteSharp.Abstractions.Views.Stars;
using EliteSharp.Abstractions.Views.Suns;
using SharpKind.Abstraction.Renditions;

namespace EliteSharp.Abstractions.Renditions;

/// <summary>
/// Everything one rendition of the game draws, written in its own assembly
/// and found at startup - the same door the missions come through. A whole
/// presentation is then an assembly rather than a branch in the game.s
/// composition root.
/// <para>
/// A rendition holds no state: it is handed an <see cref="IViewSurface"/> and
/// builds views on it, and the game keeps what it built. Unlike a mission, a
/// rendition is not optional - the tier the commander configured has to be
/// installed or there is nothing to draw with.
/// </para>
/// </summary>
public interface IRendition
{
    /// <summary>
    /// Gets which machine this rendition stands in for.
    /// <para>
    /// One of a closed set the engine defines, not a name the rendition
    /// chooses. That is what lets a config file, a folder name and a settings
    /// screen agree about what exists, and what makes Elite's 8-bit tier and
    /// another game's the same answer to the same question.
    /// </para>
    /// </summary>
    public Rendition Rendition { get; }

    /// <summary>
    /// Gets the name this rendition is known by - in the config file, and in
    /// the folder its assets sit in. Derived from
    /// <see cref="Rendition"/> rather than declared, so the two cannot
    /// disagree.
    /// </summary>
    public string Name => RenditionNames.Of(Rendition);

    /// <summary>
    /// Gets the width in pixels this rendition draws at. The game renders at
    /// this size and the window magnifies it, so a rendition picks its own
    /// resolution rather than being handed one.
    /// </summary>
    public int ScreenWidth { get; }

    /// <summary>
    /// Gets the height in pixels this rendition draws at.
    /// </summary>
    public int ScreenHeight { get; }

    /// <summary>
    /// Gets the height in pixels of the console band at the foot of the
    /// screen - the dials, the radar and the compass. The viewport is
    /// everything above it, so this is the one piece of the HUD that decides
    /// where every other screen may draw.
    /// <para>
    /// Stated rather than measured off the HUD art: that art is a full-screen
    /// overlay - the canopy the universe is seen through as well as the
    /// console under it - so its own height is the screen's and says nothing
    /// about where the band starts.
    /// </para>
    /// </summary>
    public int ConsoleHeight { get; }

    /// <summary>
    /// Gets the design-space scale: this rendition's chrome is authored in the
    /// original's 256-square space, and this multiplies those positions and
    /// sizes up to its own pixels, so chrome drawn at twice the original's
    /// proportions uses 2.
    /// <para>
    /// It is a property of the artwork rather than of the resolution, so it
    /// need not divide the screen: a rendition can draw 16-bit-proportioned
    /// chrome on a larger canvas and simply have more room around it. Whole
    /// numbers only - a fraction puts HUD text and ship vertices on
    /// half-pixels.
    /// </para>
    /// </summary>
    public int DesignScale { get; }

    /// <summary>
    /// Gets the window scales this rendition offers, smallest first. A scale
    /// magnifies the rendered pixels at presentation, so what is sensible
    /// depends on how big this rendition already draws: a 320x256 canvas has
    /// room to quadruple where a 512x512 one does not.
    /// <para>
    /// A rendition that says nothing offers only 1, which every display can
    /// show.
    /// </para>
    /// </summary>
    public IReadOnlyList<int> WindowScales => [1];

    /// <summary>
    /// Gets the window scale a commander who has never chosen one gets. Stated
    /// rather than taken as the largest of <see cref="WindowScales"/>: which
    /// window a rendition wants to open at is its own choice, not a
    /// consequence of what it will tolerate.
    /// </summary>
    public int DefaultWindowScale => 1;

    /// <summary>
    /// Gets a value indicating whether this rendition's ships are lit by a
    /// directional light rather than filled with their flat model colour.
    /// <para>
    /// It is the rendition's answer and not a setting, because it is a fact
    /// about the machine being stood in for: shading a face means colours
    /// between the model's own, and a rendition whose palette is its whole
    /// colour set has none to give. Renditions do not shade unless they say
    /// so, which is why the default is <see langword="false"/>.
    /// </para>
    /// </summary>
    public bool ShadesShips => false;

    /// <summary>
    /// Builds the chrome every screen of this tier shares - the border, the
    /// header, the countdown, the word wrap.
    /// </summary>
    public IBaseView CreateBaseView(IViewSurface surface);

    /// <summary>
    /// Builds the mission briefing screen, which is the one screen that tells
    /// the game something back: where this tier puts the ship posing behind a
    /// briefing.
    /// </summary>
    public IMissionBriefingView CreateMissionBriefingView(IViewSurface surface);

    /// <summary>
    /// Builds every other screen. The game checks the set holds all of them
    /// before it draws a frame.
    /// </summary>
    public ViewSet CreateViews(IViewSurface surface);

    /// <summary>
    /// How the settings screens look in this rendition. They have no view of
    /// their own: the game builds their controls and binds them to its own
    /// settings, and a rendition supplies only the colours and positions -
    /// the same division the planet and sun renderers use.
    /// </summary>
    /// <param name="surface">The surface whose palette the colours come from.</param>
    /// <returns>This rendition's settings list style.</returns>
    public SettingsListStyle CreateSettingsListStyle(IViewSurface surface);

    /// <summary>
    /// How the market screen looks in this rendition. It has no view of its own
    /// for the same reason the settings screens do not, and one more: the goods
    /// are a plugin, so how many rows there are is not something a rendition can
    /// be written against. It declares how many it has room for and the game's
    /// list scrolls.
    /// </summary>
    /// <param name="surface">The surface whose palette the colours come from.</param>
    /// <returns>This rendition's market list style.</returns>
    public MarketListStyle CreateMarketListStyle(IViewSurface surface);

    /// <summary>
    /// How the inventory screen looks in this rendition. It has no view of its
    /// own for the same reason the market does not: what a commander can be
    /// carrying depends on the goods set, so a rendition declares how many rows
    /// it has room for and the game's list scrolls.
    /// </summary>
    /// <param name="surface">The surface whose palette the colours come from.</param>
    /// <returns>This rendition's inventory list style.</returns>
    public InventoryListStyle CreateInventoryListStyle(IViewSurface surface);

    /// <summary>
    /// Builds a renderer for one planet. Planets live in the universe rather
    /// than on a screen - they move, they are cloned - so the game keeps the
    /// planet and the rendition supplies only what it looks like.
    /// </summary>
    /// <param name="surface">What the renderer draws on.</param>
    /// <param name="look">Which style, and what it needs to build one.</param>
    /// <returns>A renderer for that style.</returns>
    public IPlanetRenderer CreatePlanetRenderer(IViewSurface surface, PlanetLook look);

    /// <summary>
    /// Builds a renderer for one sun. As with a planet, the sun stays in the
    /// universe and only what it looks like is the rendition's.
    /// </summary>
    /// <param name="surface">What the renderer draws on.</param>
    /// <param name="look">Which style, and what it needs to build one.</param>
    /// <returns>A renderer for that style.</returns>
    public ISunRenderer CreateSunRenderer(IViewSurface surface, SunLook look);

    /// <summary>
    /// Builds the starfield renderer. There is only one - a star has no styles
    /// to pick between - so this takes no look.
    /// </summary>
    /// <param name="surface">What the renderer draws on.</param>
    /// <returns>A renderer for the starfield.</returns>
    public IStarfieldRenderer CreateStarfieldRenderer(IViewSurface surface);

    /// <summary>
    /// What this rendition paints each sort of ship. One definition serves
    /// everything that colours a ship - the scanner's lollipops and the beam a
    /// ship fires - so the two cannot drift apart.
    /// </summary>
    /// <param name="surface">The surface whose palette the colours come from.</param>
    /// <returns>This rendition's ship colours.</returns>
    public ShipColours CreateShipColours(IViewSurface surface);
}

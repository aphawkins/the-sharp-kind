// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Abstraction.Renditions;

namespace BubbleBobbleSharp.Abstractions.Renditions;

/// <summary>
/// Everything one rendition of the game draws, written in its own assembly and
/// found at startup - the same door Elite's renditions come through.
/// </summary>
public interface IBbRendition
{
    /// <summary>
    /// Gets which machine this rendition stands in for.
    /// </summary>
    public Rendition Rendition { get; }

    /// <summary>
    /// Gets the name this rendition is known by, in the config file and in the
    /// folder its assets sit in. Derived, so the two cannot disagree.
    /// </summary>
    public string Name => RenditionNames.Of(Rendition);

    /// <summary>
    /// Gets the width in pixels this rendition draws at. The game renders at
    /// this size and the window magnifies it.
    /// </summary>
    public int ScreenWidth { get; }

    /// <summary>
    /// Gets the height in pixels this rendition draws at.
    /// </summary>
    public int ScreenHeight { get; }

    /// <summary>
    /// Gets the window scales this rendition offers, smallest first.
    /// </summary>
    public IReadOnlyList<int> WindowScales => [1];

    /// <summary>
    /// Gets the window scale a player who has never chosen one gets.
    /// </summary>
    public int DefaultWindowScale => 1;

    /// <summary>
    /// Creates the view that draws the decoration down both edges of the level.
    /// </summary>
    /// <param name="surface">Everything the view draws with.</param>
    /// <returns>The view.</returns>
    public IView<SidebarModel> CreateSidebarView(IViewSurface surface);

    /// <summary>
    /// Creates the view that draws the level itself.
    /// </summary>
    /// <param name="surface">Everything the view draws with.</param>
    /// <returns>The view.</returns>
    public IView<PlayfieldModel> CreatePlayfieldView(IViewSurface surface);

    /// <summary>
    /// Creates the view that draws the scores and lives beside the level.
    /// </summary>
    /// <param name="surface">Everything the view draws with.</param>
    /// <returns>The view.</returns>
    public IView<HudModel> CreateHudView(IViewSurface surface);
}

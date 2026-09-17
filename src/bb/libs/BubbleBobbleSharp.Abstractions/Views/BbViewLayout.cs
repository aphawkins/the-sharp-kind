// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Graphics;

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>
/// The rendition's screen metrics, in one place, for the views to lay out
/// against.
/// <para>
/// The C64's screen is 40 columns by 25 rows of 8x8 characters. The level
/// occupies the leftmost 32 of those columns, because that is where
/// setup_level_screen writes it: it starts each row at the screen pointer's
/// own offset 0 and stops after 32 columns, and draw_border then clears
/// column 32 as the first column past it. The eight columns to the right of
/// that are not the level's.
/// </para>
/// <para>
/// The metrics are settable rather than fixed. A rendition of another tier
/// would have a cell size of its own, and this type is handed to every
/// rendition, so baking the C64's numbers in would decide for renditions that
/// do not exist yet.
/// </para>
/// </summary>
/// <param name="ScreenWidth">The render width in pixels.</param>
/// <param name="ScreenHeight">The render height in pixels.</param>
public sealed record BbViewLayout(float ScreenWidth, float ScreenHeight)
    : ScreenLayout(ScreenWidth, ScreenHeight)
{
    /// <summary>Gets the width of one character cell in pixels.</summary>
    public float CellWidth { get; init; } = 8;

    /// <summary>Gets the height of one character cell in pixels.</summary>
    public float CellHeight { get; init; } = 8;

    /// <summary>Gets the columns the level is drawn in, of the screen's forty.</summary>
    public int PlayfieldColumns { get; init; } = 32;

    /// <summary>Gets the rows the level is drawn in - the whole screen's worth.</summary>
    public int PlayfieldRows { get; init; } = 25;

    /// <summary>
    /// Gets the screen column the level starts at. Zero: the level renderer
    /// writes from the start of each screen row, so the level is hard against
    /// the left edge rather than centred.
    /// </summary>
    public int PlayfieldColumn { get; init; }

    /// <summary>Gets the screen row the level starts at.</summary>
    public int PlayfieldRow { get; init; }

    /// <summary>
    /// Gets the whole screen's character grid, counted from its top-left
    /// corner - the forty by twenty-five the hardware addresses.
    /// </summary>
    public CharacterGrid Screen => new(CellWidth, CellHeight);

    /// <summary>
    /// Gets the level's character grid, counted from the level's own top-left
    /// corner rather than the screen's, so a view says which column of the
    /// level it means and nothing has to add the offset back on.
    /// </summary>
    public CharacterGrid Playfield => new(
        CellWidth,
        CellHeight,
        new(Screen.Column(PlayfieldColumn), Screen.Row(PlayfieldRow)));
}

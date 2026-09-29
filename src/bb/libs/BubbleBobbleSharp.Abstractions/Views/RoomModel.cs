// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>What a bonus room puts over the level: its items, the clock and the counts, and the results.</summary>
/// <param name="Cells">The level cell of the top-left character of each item still to take, as (column, row).</param>
/// <param name="Art">The block of item art every item is drawn from.</param>
/// <param name="ColourRam">The colour RAM byte the items are in.</param>
/// <param name="Colours">The level's colour byte, which the items' two shared colours are.</param>
/// <param name="Tile">The level's own tile, counted from zero, which frames the results' window.</param>
/// <param name="Lines">The text over the level.</param>
/// <param name="Box">Whether the results' window is up, which has the level's tile for a frame and a black inside.</param>
public sealed record RoomModel(
    IReadOnlyList<(int Column, int Row)> Cells,
    int Art,
    int ColourRam,
    int Colours,
    int Tile,
    IReadOnlyList<RoomText> Lines,
    bool Box)
{
    /// <summary>Gets the columns of the results' window, from its frame's left.</summary>
    public static int BoxColumn => 6;

    /// <summary>Gets the rows of the results' window, from its frame's top.</summary>
    public static int BoxRow => 5;

    /// <summary>Gets the width of the results' window, in columns, with its frame.</summary>
    public static int BoxWidth => 20;

    /// <summary>Gets the height of the results' window, in rows, with its frame.</summary>
    public static int BoxHeight => 10;
}

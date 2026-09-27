// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The rendition's screen metrics, in one place, for the views to lay out against.</summary>
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

    /// <summary>Gets the screen column the level starts at.</summary>
    public int PlayfieldColumn { get; init; }

    /// <summary>Gets the screen row the level starts at.</summary>
    public int PlayfieldRow { get; init; }

    /// <summary>Gets how many columns of the level each edge's decoration covers.</summary>
    public int SidebarColumns { get; init; } = 2;

    /// <summary>Gets the columns the HUD is drawn in.</summary>
    public int HudColumns { get; init; } = 8;

    /// <summary>Gets the whole screen's character grid.</summary>
    public CharacterGrid Screen => new(CellWidth, CellHeight);

    /// <summary>Gets the level's character grid, counted from the level's top-left corner.</summary>
    public CharacterGrid Playfield => new(
        CellWidth,
        CellHeight,
        new(Screen.Column(PlayfieldColumn), Screen.Row(PlayfieldRow)));

    /// <summary>Gets the whole level as a rectangle on the screen.</summary>
    public (Vector2 Position, float Width, float Height) PlayfieldArea => (
        Playfield.Cell(0, 0),
        PlayfieldColumns * CellWidth,
        PlayfieldRows * CellHeight);

    /// <summary>Gets the level between its two edges of decoration.</summary>
    public (Vector2 Position, float Width, float Height) PlayfieldInterior => (
        Playfield.Cell(SidebarColumns, 0),
        (PlayfieldColumns - (2 * SidebarColumns)) * CellWidth,
        PlayfieldRows * CellHeight);

    /// <summary>Gets the HUD as a rectangle on the screen.</summary>
    public (Vector2 Position, float Width, float Height) HudArea => (
        Screen.Cell(PlayfieldColumn + PlayfieldColumns, PlayfieldRow),
        HudColumns * CellWidth,
        PlayfieldRows * CellHeight);
}

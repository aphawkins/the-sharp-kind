// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The HURRY UP! text (<c>$3AB8</c>) on its way up the playfield.</summary>
/// <param name="Row">The screen row it is on, 0 being the topmost.</param>
/// <param name="ColourRam">Its colour RAM byte, whose low three bits are entry 11.</param>
/// <param name="Backed">Whether the level is cleared to black around its letters, as it is at row 11.</param>
public readonly record struct HurryText(int Row, int ColourRam, bool Backed)
{
    /// <summary>Gets the first of the columns it is drawn in.</summary>
    public static int FirstColumn => 12;

    /// <summary>Gets how many characters it is.</summary>
    public static int Length => 9;
}

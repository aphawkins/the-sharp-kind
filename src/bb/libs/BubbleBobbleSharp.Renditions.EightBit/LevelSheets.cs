// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>A level sheet, with a copy of its own for the level that comes in under the split as the screen scrolls.</summary>
/// <remarks>The scroll to the next level ($37C9) shows the old level over the new, each in its own colours.</remarks>
internal sealed class LevelSheets(IViewSurface surface, string source)
{
    private readonly MulticolourSheet _sheet = new(surface, source);
    private readonly MulticolourSheet _incoming = new(surface, source, source + "Incoming");

    /// <summary>Gets the sheet painted in a level's colours.</summary>
    /// <param name="colours">The level's colour byte.</param>
    /// <param name="incoming">Whether the level is the one coming in under the split.</param>
    /// <returns>The name to draw from.</returns>
    internal string Paint(int colours, bool incoming)
    {
        MulticolourSheet sheet = incoming ? _incoming : _sheet;

        sheet.Paint(colours);

        return sheet.Name;
    }
}

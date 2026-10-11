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
    private readonly MulticolourSheet _wiped = new(surface, source, source + "Wiped");

    /// <summary>Gets the sheet painted in a level's colours.</summary>
    /// <param name="colours">The level's colour byte.</param>
    /// <param name="incoming">Whether the level is the one coming in under the split.</param>
    /// <param name="colourRam">The cells' colour RAM byte.</param>
    /// <returns>The name to draw from.</returns>
    internal string Paint(int colours, bool incoming, int colourRam = MulticolourSheet.PlayfieldColourRam)
    {
        MulticolourSheet sheet = incoming ? _incoming : _sheet;

        sheet.Paint(colours, colourRam);

        return sheet.Name;
    }

    /// <summary>Gets the sheet painted in a level's colours with a wiped row's colour RAM.</summary>
    /// <param name="colours">The level's colour byte.</param>
    /// <returns>The name to draw from.</returns>
    internal string PaintWiped(int colours)
    {
        _wiped.Paint(colours, PlayfieldModel.WipedColourRam);

        return _wiped.Name;
    }
}

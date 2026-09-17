// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Assets.Palettes;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>
/// One of the playfield's multicolour sheets, painted in the colours the level
/// wears.
/// <para>
/// A multicolour character is four two-bit entry numbers a row, not four
/// colours: 00 is the background, 01 and 10 are the two background registers
/// the VIC-II holds for the whole screen, and 11 is the character's own colour
/// RAM nibble. The sheets are exported with the entry number written as
/// palette colour 0, 1, 2 or 3, so a sheet as committed is the level's shapes
/// in the wrong colours - and the right ones are whatever the level's colour
/// byte points those entries at.
/// </para>
/// <para>
/// So the sheet is repainted once a level and the repainted copy is held under
/// a name of its own for as long as the level lasts. That beats exporting a
/// copy per colour combination, and it beats resolving an entry per pixel at
/// draw time, which is the thing a byte-per-pixel sheet would cost every
/// frame.
/// </para>
/// </summary>
internal sealed class MulticolourSheet
{
    // Where a repainted sheet lives, beside the one it was painted from.
    private const string Suffix = ".Recoloured";

    // $09B8, game-loop.s: colour RAM is filled with $0D before the level is drawn. Bit 3 of that is
    // what puts the cell in multicolour mode, and only the low three bits are left to be a colour -
    // so entry 11 is colour 5 on every cell of the playfield.
    private const int ColourRam = 0x0D & 0x07;

    // No level's colour byte is negative, so the first draw always repaints.
    private const int NotPainted = -1;

    private readonly IGraphics _graphics;
    private readonly IPaletteCollection _palette;
    private readonly string _source;

    private int _colours = NotPainted;

    internal MulticolourSheet(IViewSurface surface, string source)
    {
        _graphics = surface.Graphics;
        _palette = surface.Palette;
        _source = source;

        Name = source + Suffix;
    }

    /// <summary>
    /// Gets the name the repainted sheet is drawn by. It holds nothing until
    /// <see cref="Paint"/> has been called once.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// Repaints the sheet for a level's colour byte, unless it is already
    /// wearing it.
    /// </summary>
    /// <param name="colours">The level's colour byte.</param>
    internal void Paint(int colours)
    {
        if (colours == _colours)
        {
            return;
        }

        _graphics.SetImage(Name, _graphics.Image(_source).Recolour(Entries(colours)));
        _colours = colours;
    }

    // $E0CE, level-renderer.s: the high nibble goes to $1D and the low to $1F, and the split-screen
    // IRQ at $072E writes those to the two background registers in that order. Entry 00 is the
    // screen's own background, which the sheets carry as a transparent pixel and nothing repaints.
    private Dictionary<uint, FastColor> Entries(int colours) => new()
    {
        [Colour(1).Argb] = Colour((colours >> 4) & 0x0F),
        [Colour(2).Argb] = Colour(colours & 0x0F),
        [Colour(3).Argb] = Colour(ColourRam),
    };

    private FastColor Colour(int index) => _palette[index.ToString(CultureInfo.InvariantCulture)];
}

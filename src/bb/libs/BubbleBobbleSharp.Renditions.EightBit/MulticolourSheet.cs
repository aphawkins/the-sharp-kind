// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Assets.Palettes;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>One of the playfield's multicolour sheets, painted in the colours the level wears.</summary>
internal sealed class MulticolourSheet
{
    internal const int PlayfieldColourRam = 0x0D;

    private const string Suffix = ".Recoloured";

    private const int NotPainted = -1;

    private readonly IGraphics _graphics;
    private readonly IPaletteCollection _palette;
    private readonly string _source;

    private int _colours = NotPainted;
    private int _colourRam = NotPainted;

    internal MulticolourSheet(IViewSurface surface, string source, string? name = null)
    {
        _graphics = surface.Graphics;
        _palette = surface.Palette;
        _source = source;

        Name = (name ?? source) + Suffix;
    }

    /// <summary>Gets the name the repainted sheet is drawn by.</summary>
    internal string Name { get; }

    /// <summary>Repaints the sheet for a level's colour byte, unless it is already wearing it.</summary>
    /// <param name="colours">The level's colour byte.</param>
    /// <param name="colourRam">The cells' colour RAM byte, whose low three bits are entry 11.</param>
    internal void Paint(int colours, int colourRam = PlayfieldColourRam)
    {
        if (colours == _colours && colourRam == _colourRam)
        {
            return;
        }

        _graphics.SetImage(Name, _graphics.Image(_source).Recolour(Entries(colours, colourRam)));
        _colours = colours;
        _colourRam = colourRam;
    }

    private Dictionary<uint, FastColor> Entries(int colours, int colourRam) => new()
    {
        [Colour(1).Argb] = Colour((colours >> 4) & 0x0F),
        [Colour(2).Argb] = Colour(colours & 0x0F),
        [Colour(3).Argb] = Colour(colourRam & 0x07),
    };

    private FastColor Colour(int index) => _palette[index.ToString(CultureInfo.InvariantCulture)];
}

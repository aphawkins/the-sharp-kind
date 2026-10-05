// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Assets.Palettes;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>The game's sprite sheet, painted for one sprite's colour.</summary>
internal sealed class MulticolourSprites
{
    private const int MulticolourZero = 2;
    private const int MulticolourOne = 1;

    private readonly IGraphics _graphics;
    private readonly IPaletteCollection _palette;
    private readonly string _source;
    private readonly bool _itemArt;
    private readonly Dictionary<int, string> _painted = [];

    internal MulticolourSprites(IViewSurface surface, string source, bool itemArt = false)
    {
        _graphics = surface.Graphics;
        _palette = surface.Palette;
        _source = source;
        _itemArt = itemArt;
    }

    /// <summary>The sheet painted in one sprite's colour.</summary>
    /// <param name="colour">The sprite's own colour.</param>
    /// <returns>The image name to draw from.</returns>
    internal string Paint(int colour)
    {
        if (_painted.TryGetValue(colour, out string? name))
        {
            return name;
        }

        name = $"{_source}.Colour{colour.ToString(CultureInfo.InvariantCulture)}";
        _graphics.SetImage(name, _graphics.Image(_source).Recolour(Entries(colour)));
        _painted.Add(colour, name);

        return name;
    }

    // The item art is character art, and `$2996` turns a character's bit pairs into a sprite's: 10 and 11 change places, so
    // the character's second background is the sprite's second shared colour and its colour RAM the sprite's own. The
    // character's 00 is the screen's black, but a sprite's 00 is transparent.
    private Dictionary<uint, FastColor> Entries(int colour) => _itemArt
        ? new()
        {
            [Colour(0).Argb] = BaseColors.TransparentBlack,
            [Colour(1).Argb] = Colour(MulticolourZero),
            [Colour(2).Argb] = Colour(MulticolourOne),
            [Colour(3).Argb] = Colour(colour),
        }
        : new()
        {
            [Colour(1).Argb] = Colour(MulticolourZero),
            [Colour(2).Argb] = Colour(colour),
            [Colour(3).Argb] = Colour(MulticolourOne),
        };

    private FastColor Colour(int index) => _palette[index.ToString(CultureInfo.InvariantCulture)];
}

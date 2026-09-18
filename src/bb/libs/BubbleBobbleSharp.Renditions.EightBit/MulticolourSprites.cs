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
/// The game's sprite sheet, painted for one sprite's colour.
/// <para>
/// A multicolour sprite is twelve two-bit entry numbers a row rather than
/// twelve colours, and the VIC-II resolves them differently from the way it
/// resolves a character's. 00 is transparent; 01 and 11 come from $D025 and
/// $D026, which are one pair for every sprite on the screen; and 10 is the
/// sprite's own colour register, which is the only one of the three a sprite
/// chooses for itself. That inversion - the sprite's own colour in the middle
/// rather than at the top - is why this cannot be <see cref="MulticolourSheet"/>
/// with different arguments.
/// </para>
/// <para>
/// The two shared registers are set once, at $44E5, and never written again:
/// $D026 is 1 and $D025 is 2. So what varies between one sprite and the next is
/// the single entry, and a repainted copy is held per colour rather than per
/// sprite - two of them, in practice, because the two players are the only
/// things this draws so far and they wear 5 and 3.
/// </para>
/// </summary>
internal sealed class MulticolourSprites
{
    // $44E9 and $44EC. X is 1 when $D026 is written and 2 when $D025 is, and nothing writes either
    // again: every multicolour sprite in the game shares this pair.
    private const int MulticolourZero = 2;
    private const int MulticolourOne = 1;

    private readonly IGraphics _graphics;
    private readonly IPaletteCollection _palette;
    private readonly string _source;
    private readonly Dictionary<int, string> _painted = [];

    internal MulticolourSprites(IViewSurface surface, string source)
    {
        _graphics = surface.Graphics;
        _palette = surface.Palette;
        _source = source;
    }

    /// <summary>
    /// The name a copy of the sheet in one sprite's colour is drawn by,
    /// painting it the first time that colour is asked for.
    /// </summary>
    /// <param name="colour">The sprite's own colour, as $8548 holds it.</param>
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

    // Entry 00 is the sheet's transparent pixel and nothing repaints it - the exporter writes it
    // with no alpha, which is the VIC-II's own reading of a sprite's 00.
    private Dictionary<uint, FastColor> Entries(int colour) => new()
    {
        [Colour(1).Argb] = Colour(MulticolourZero),
        [Colour(2).Argb] = Colour(colour),
        [Colour(3).Argb] = Colour(MulticolourOne),
    };

    private FastColor Colour(int index) => _palette[index.ToString(CultureInfo.InvariantCulture)];
}

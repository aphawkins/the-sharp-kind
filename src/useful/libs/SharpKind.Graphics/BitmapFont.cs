// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Assets;

namespace SharpKind.Graphics;

// A font sheet plus the layout needed to find a glyph in it. Two shapes are
// supported, and they differ in ways that matter typographically rather than
// incidentally:
//
// - Proportional sheets (the 16-bit fonts) pack variable-width glyphs into a
//   fixed cell and terminate each one with a magenta marker pixel, so the
//   width has to be measured per glyph. Ink is cyan and is recoloured to
//   whatever colour the caller asked for; anything else is copied through,
//   which is what lets a glyph carry more than one colour.
// - Grid sheets (the 8-bit BBC Micro font) are monospaced, exactly as the
//   hardware they imitate was: every glyph fills its cell, there are no
//   markers, and the sheet is two colours - ink and background.
public sealed class BitmapFont
{
    public BitmapFont(FastBitmap image, BitmapFontAsset asset)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(asset);

        int required = asset.Columns * asset.CellWidth;
        if (image.Width < required)
        {
            throw new SharpKindException(
                $"Font sheet is {image.Width}px wide, too narrow for {asset.Columns} columns of {asset.CellWidth}px.");
        }

        Image = image;
        CellWidth = asset.CellWidth;
        CellHeight = asset.CellHeight;
        Columns = asset.Columns;
        IsProportional = asset.IsProportional;
        Rows = image.Height / asset.CellHeight;
    }

    public FastBitmap Image { get; }

    public int CellWidth { get; }

    public int CellHeight { get; }

    public int Columns { get; }

    public int Rows { get; }

    public bool IsProportional { get; }

    // Cyan on a proportional sheet, white on a two-colour grid sheet: the
    // pixels that take on the requested text colour.
    public FastColor Ink => IsProportional ? BaseColors.Cyan : BaseColors.White;

    // Grid sheets are opaque, so their background colour is the transparency
    // key; proportional sheets already carry an alpha channel.
    public FastColor Background => IsProportional ? BaseColors.TransparentBlack : BaseColors.Black;

    // A sheet only carries the cells its image has room for. Anything before
    // space, or past the last cell, has no glyph - and reading its cell would
    // read outside the image.
    public bool Has(char letter)
    {
        int index = letter - ' ';
        return index >= 0 && index < Columns * Rows;
    }

    // Glyphs run from space (ASCII 32) left to right, top to bottom. This is
    // the same mapping the proportional sheets always used - (c >> 4) - 2 and
    // c & 0xF are just this arithmetic with Columns fixed at 16.
    public (int X, int Y) CellOrigin(char letter)
    {
        int index = letter - ' ';
        int column = index % Columns;
        int row = index / Columns;
        return (column * CellWidth, row * CellHeight);
    }
}

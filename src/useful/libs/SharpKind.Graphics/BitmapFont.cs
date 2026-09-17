// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Assets;

namespace SharpKind.Graphics;

// A font sheet plus the layout needed to find a glyph in it. Proportional sheets (16-bit fonts) pack
// variable-width glyphs into a fixed cell, terminated by a magenta marker; cyan ink recolours, everything else
// copies through (letting a glyph carry more than one colour). Grid sheets (8-bit BBC Micro font) are
// monospaced, two colours only, no markers.
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

    // Cyan on a proportional sheet, white on a grid sheet - the pixels that take the requested text colour.
    public FastColor Ink => IsProportional ? BaseColors.Cyan : BaseColors.White;

    // Grid sheets are opaque, so their background colour is the transparency key; proportional sheets already carry an alpha channel.
    public FastColor Background => IsProportional ? BaseColors.TransparentBlack : BaseColors.Black;

    // A sheet only carries cells its image has room for; anything outside that range has no glyph and would read outside the image.
    public bool Has(char letter)
    {
        int index = letter - ' ';
        return index >= 0 && index < Columns * Rows;
    }

    // Glyphs run from space (ASCII 32) left to right, top to bottom; Columns fixed at 16 matches the classic (c >> 4) - 2 / c & 0xF layout.
    public (int X, int Y) CellOrigin(char letter)
    {
        int index = letter - ' ';
        int column = index % Columns;
        int row = index / Columns;
        return (column * CellWidth, row * CellHeight);
    }
}

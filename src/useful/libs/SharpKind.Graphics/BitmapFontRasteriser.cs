// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;

namespace SharpKind.Graphics;

/// <summary>
/// Draws text from the renditions' own font sheets - the artwork each
/// rendition ships, in the two shapes <see cref="BitmapFont"/> describes.
/// </summary>
public sealed class BitmapFontRasteriser : IFontRasteriser
{
    private readonly Dictionary<string, BitmapFont> _fonts;

    public BitmapFontRasteriser(Dictionary<string, BitmapFont> fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);

        _fonts = fonts;
    }

    public FastBitmap Rasterise(string text, string fontType, FastColor color)
    {
        ArgumentNullException.ThrowIfNull(text);

        BitmapFont font = _fonts[fontType];

        // Sized for the worst case (every glyph as wide as its cell), then cut down once actual widths are known for proportional sheets.
        using FastBitmap temp = new(text.Length * font.CellWidth, font.CellHeight);
        int totalWidth = 0;

        foreach (char letter in text)
        {
            totalWidth += !font.Has(letter) ? MissingWidth(font)
                : font.IsProportional ? AppendProportionalGlyph(font, temp, totalWidth, letter, color)
                : AppendGridGlyph(font, temp, totalWidth, letter, color);
        }

        return temp.Resize(totalWidth, font.CellHeight);
    }

    // Measured from the sheet directly - going via the backends' colour-keyed text cache would fill it with an entry per colour ever measured in.
    public Vector2 Measure(string text, string fontType)
    {
        ArgumentNullException.ThrowIfNull(text);

        BitmapFont font = _fonts[fontType];

        if (string.IsNullOrWhiteSpace(text))
        {
            return new(0, font.CellHeight);
        }

        int width = 0;
        foreach (char letter in text)
        {
            width += !font.Has(letter) ? MissingWidth(font)
                : font.IsProportional ? ProportionalGlyphWidth(font, letter)
                : font.CellWidth;
        }

        return new(width, font.CellHeight);
    }

    // A character with no cell leaves a space-width gap rather than a substitute glyph (unlike the .fon rasteriser), keeping the line readable.
    private static int MissingWidth(BitmapFont font)
        => font.IsProportional ? ProportionalGlyphWidth(font, ' ') : font.CellWidth;

    // Ink takes the requested colour, background becomes transparent, everything else copies through - letting a glyph carry more than one colour.
    private static FastColor Recolour(BitmapFont font, int x, int y, in FastColor color)
    {
        FastColor pixelColor = font.Image.GetPixel(x, y);

        return pixelColor == font.Ink ? color
            : pixelColor == font.Background ? BaseColors.TransparentBlack
            : pixelColor;
    }

    // Monospaced: every glyph fills its cell, so there is nothing to measure
    // and no marker to look for.
    private static int AppendGridGlyph(BitmapFont font, FastBitmap temp, int left, char letter, in FastColor color)
    {
        (int originX, int originY) = font.CellOrigin(letter);

        for (int y = 0; y < font.CellHeight; y++)
        {
            for (int x = 0; x < font.CellWidth; x++)
            {
                temp.SetPixel(left + x, y, Recolour(font, originX + x, originY + y, color));
            }
        }

        return font.CellWidth;
    }

    // Width half of AppendProportionalGlyph; Recolour leaves magenta alone so markers stay put regardless of text colour.
    private static int ProportionalGlyphWidth(BitmapFont font, char letter)
    {
        (int originX, int originY) = font.CellOrigin(letter);
        int charX = 0;
        int charY = 0;
        int maxCharWidth = 0;

        do
        {
            do
            {
                charX++;
            }
            while (font.Image.GetPixel(originX + charX, originY + charY) != BaseColors.Magenta);

            maxCharWidth = Math.Max(maxCharWidth, charX);
            charX = 0;
            charY++;
        }
        while (font.Image.GetPixel(originX, originY + charY) != BaseColors.Magenta);

        return maxCharWidth;
    }

    // Variable width: a magenta marker ends each row of the glyph, and a
    // magenta pixel where the next row would start ends the glyph.
    private static int AppendProportionalGlyph(BitmapFont font, FastBitmap temp, int left, char letter, in FastColor color)
    {
        (int originX, int originY) = font.CellOrigin(letter);
        int charX = 0;
        int charY = 0;
        int maxCharWidth = 0;
        FastColor pixelColor = Recolour(font, originX, originY, color);

        do
        {
            do
            {
                temp.SetPixel(left + charX, charY, pixelColor);
                charX++;
                pixelColor = Recolour(font, originX + charX, originY + charY, color);
            }
            while (pixelColor != BaseColors.Magenta);

            maxCharWidth = Math.Max(maxCharWidth, charX);
            charX = 0;
            charY++;

            pixelColor = Recolour(font, originX, originY + charY, color);
        }
        while (pixelColor != BaseColors.Magenta);

        return maxCharWidth;
    }
}

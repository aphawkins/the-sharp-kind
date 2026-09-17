// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using SharpKind.Assets;

namespace SharpKind.Graphics;

/// <summary>
/// Draws text from Windows .fon strikes. A strike is one bit per pixel, so
/// the ink takes the requested colour and everything else stays transparent -
/// the same two-colour result the renditions' own grid sheets give, from a
/// font that was never drawn for this game.
/// </summary>
public sealed class FonFontRasteriser : IFontRasteriser
{
    private readonly Dictionary<string, FonFont> _fonts;

    public FonFontRasteriser(Dictionary<string, FonFont> fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);

        _fonts = fonts;
    }

    public FastBitmap Rasterise(string text, string fontType, FastColor color)
    {
        ArgumentNullException.ThrowIfNull(text);

        FonFont font = _fonts[fontType];

        // As the sheet rasteriser does: sized for the widest each glyph could be, then cut down to what they took.
        using FastBitmap temp = new(Math.Max(text.Length * font.MaxWidth, 1), font.PixelHeight);
        int left = 0;

        foreach (char letter in text)
        {
            FonGlyph? glyph = font.Glyph(letter);

            if (glyph is null)
            {
                left += MissingWidth(font);
                continue;
            }

            for (int y = 0; y < glyph.Height; y++)
            {
                for (int x = 0; x < glyph.Width; x++)
                {
                    temp.SetPixel(left + x, y, glyph[x, y] ? color : BaseColors.TransparentBlack);
                }
            }

            left += glyph.Width;
        }

        return temp.Resize(left, font.PixelHeight);
    }

    public Vector2 Measure(string text, string fontType)
    {
        ArgumentNullException.ThrowIfNull(text);

        FonFont font = _fonts[fontType];

        if (string.IsNullOrWhiteSpace(text))
        {
            return new(0, font.PixelHeight);
        }

        int width = 0;
        foreach (char letter in text)
        {
            width += font.Glyph(letter)?.Width ?? MissingWidth(font);
        }

        return new(width, font.PixelHeight);
    }

    // A character the strike doesn't cover leaves a gap, not a substitute glyph - inventing one would hide that
    // a fuller font is needed. Gap width is a space where the strike has one, else the widest glyph.
    private static int MissingWidth(FonFont font) => font.Glyph(' ')?.Width ?? font.MaxWidth;
}

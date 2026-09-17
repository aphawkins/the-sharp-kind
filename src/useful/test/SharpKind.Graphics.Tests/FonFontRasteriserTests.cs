// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Assets;

namespace SharpKind.Graphics.Tests;

// Drawing text from a .fon strike: ink takes the requested colour, paper
// stays transparent, and the glyphs advance by their own widths.
public class FonFontRasteriserTests
{
    [Fact]
    public void DrawsInkInTheRequestedColourOverTransparentPaper()
    {
        // Arrange - 'A' as a 2x2 glyph inked down its left column.
        FonFontRasteriser rasteriser = Rasteriser(('A', 2, [true, false, true, false]));

        // Act
        using FastBitmap bitmap = rasteriser.Rasterise("A", "TestFont", BaseColors.Red);

        // Assert
        Assert.Equal(BaseColors.Red, bitmap.GetPixel(0, 0));
        Assert.Equal(BaseColors.Red, bitmap.GetPixel(0, 1));
        Assert.Equal(BaseColors.TransparentBlack, bitmap.GetPixel(1, 0));
        Assert.Equal(BaseColors.TransparentBlack, bitmap.GetPixel(1, 1));
    }

    // A proportional strike's glyphs each advance by their own width, so a
    // narrow glyph must not leave the gap a monospaced advance would.
    [Fact]
    public void AdvancesByEachGlyphsOwnWidth()
    {
        // Arrange - a 1px 'i' fully inked, then a 3px 'j' fully inked.
        FonFontRasteriser rasteriser = Rasteriser(
            ('i', 1, [true, true]),
            ('j', 3, [true, true, true, true, true, true]));

        // Act
        using FastBitmap bitmap = rasteriser.Rasterise("ij", "TestFont", BaseColors.White);

        // Assert - four pixels wide, with no transparent column between them.
        Assert.Equal(4, bitmap.Width);

        for (int x = 0; x < 4; x++)
        {
            Assert.Equal(BaseColors.White, bitmap.GetPixel(x, 0));
        }
    }

    [Fact]
    public void MeasuresWithoutDrawing()
    {
        // Arrange
        FonFontRasteriser rasteriser = Rasteriser(
            ('i', 1, [true, true]),
            ('j', 3, [true, true, true, true, true, true]));

        // Act & Assert
        Assert.Equal(new(4, 2), rasteriser.Measure("ij", "TestFont"));
        Assert.Equal(new(1, 2), rasteriser.Measure("i", "TestFont"));
    }

    // Whitespace-only text measures as the line height by zero width, which
    // is what IGraphics.MeasureText documents for every font kind.
    [Fact]
    public void MeasuresWhitespaceAsTheLineHeightByNoWidth()
    {
        // Arrange
        FonFontRasteriser rasteriser = Rasteriser(('A', 2, [true, false, true, false]));

        // Act & Assert
        Assert.Equal(new(0, 2), rasteriser.Measure("   ", "TestFont"));
    }

    // A gap's width must be the same whether text is measured or drawn, or the two disagree about layout.
    [Fact]
    public void LeavesAGapForACharacterTheStrikeDoesNotCover()
    {
        // Arrange - 'A' and 'B' only, so 'C' is outside the range.
        FonFontRasteriser rasteriser = Rasteriser(
            ('A', 2, [true, true, true, true]),
            ('B', 2, [true, true, true, true]));

        // Act
        using FastBitmap bitmap = rasteriser.Rasterise("AC", "TestFont", BaseColors.White);

        // Assert - the missing glyph took the widest glyph's width, drawn as
        // nothing, and measuring agrees with what was drawn.
        Assert.Equal(4, bitmap.Width);
        Assert.Equal(new(4, 2), rasteriser.Measure("AC", "TestFont"));
        Assert.Equal(BaseColors.White, bitmap.GetPixel(0, 0));
        Assert.Equal(BaseColors.TransparentBlack, bitmap.GetPixel(2, 0));
        Assert.Equal(BaseColors.TransparentBlack, bitmap.GetPixel(3, 0));
    }

    // Builds a strike over a contiguous run of characters, given each one's
    // width and its ink row by row.
    private static FonFontRasteriser Rasteriser(params (char Letter, int Width, bool[] Ink)[] glyphs)
    {
        char firstChar = glyphs[0].Letter;
        char lastChar = glyphs[^1].Letter;
        int height = glyphs[0].Ink.Length / glyphs[0].Width;

        FonGlyph[] built = [.. glyphs.Select(x => new FonGlyph(x.Width, x.Ink.Length / x.Width, x.Ink))];

        FonFont font = new(height, glyphs.Max(x => x.Width), firstChar, lastChar, true, built);

        return new(new() { { "TestFont", font } });
    }
}

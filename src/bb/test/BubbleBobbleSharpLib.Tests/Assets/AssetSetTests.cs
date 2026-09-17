// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Assets;
using SharpKind.Graphics;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Assets;

// Loading the whole set is the artwork half's real check: it opens every file the manifest names, and the
// colour budget it measures is the C64's - 16 colours, every one of them named by palette.json.
[Trait("Level", "Integration")]
public sealed class AssetSetTests
{
    private const string Rendition = "8-bit";

    private static readonly AssetSet s_assets = AssetSet.Load(AssetLocator.CreateFrom(
        Path.Combine(AppContext.BaseDirectory, "Renditions", "BubbleBobbleSharp.Renditions.EightBit"),
        Rendition));

    [Fact]
    public void LoadsEverySheetTheManifestNames()
    {
        Assert.Equal(12, s_assets.Images.Count);
        Assert.All(s_assets.Images.Values, x => Assert.True(x.Width > 0 && x.Height > 0));
    }

    [Fact]
    public void StaysInsideTheCommodoresPalette()
    {
        Assert.True(s_assets.Budget.IsWithinBudget);
        Assert.True(s_assets.Budget.IsWithinPalette);
        Assert.True(s_assets.Budget.IsOnColourGrid);
        Assert.Equal(0, s_assets.Budget.PartialAlphaCount);
    }

    // The sheets are C64 multicolour, where bit-pair 00 is the background showing through rather than a
    // colour. The export writes that index with alpha 0, and this is what proves it survived the round trip.
    // Counted rather than probed at a fixed pixel: which corner a sheet happens to start on is not the point,
    // and a sheet that lost its transparency would come back fully opaque.
    [Theory]
    [InlineData("LevelTiles")]
    [InlineData("SpritesGame")]
    [InlineData("Sidebars")]
    [InlineData("TileEdges")]
    public void LeavesTheBackgroundIndexTransparent(string sheet)
    {
        FastBitmap image = s_assets.Images[sheet];
        int transparent = 0;
        int opaque = 0;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (image.GetPixel(x, y).Argb >> 24 == 0)
                {
                    transparent++;
                }
                else
                {
                    opaque++;
                }
            }
        }

        Assert.True(transparent > 0, $"{sheet} has no transparent pixels, so its background index was lost.");
        Assert.True(opaque > 0, $"{sheet} is entirely transparent.");
    }

    [Fact]
    public void ReadsTheGamesOwnFont()
    {
        BitmapFont font = s_assets.BitmapFonts["Small"];

        Assert.Equal(8, font.CellWidth);
        Assert.Equal(8, font.CellHeight);
        Assert.Equal(16, font.Columns);
        Assert.Equal(4, font.Rows);
        Assert.False(font.IsProportional);
    }

    // The game's charset is in its own order - digits first, letters where ASCII minus 32 happens to put
    // them - so the export reorders it. A glyph sheet that was copied across unchanged would fail here.
    [Theory]
    [InlineData('0')]
    [InlineData('9')]
    [InlineData('A')]
    [InlineData('Z')]
    public void PutsGlyphsWhereBitmapFontLooksForThem(char letter)
    {
        BitmapFont font = s_assets.BitmapFonts["Small"];

        Assert.True(font.Has(letter));

        (int x, int y) = font.CellOrigin(letter);

        Assert.True(AnyInkIn(font, x, y), $"'{letter}' should have ink at its cell, but the cell is blank.");
    }

    [Fact]
    public void LeavesUnmappedCellsBlank()
    {
        BitmapFont font = s_assets.BitmapFonts["Small"];

        (int x, int y) = font.CellOrigin(' ');

        Assert.False(AnyInkIn(font, x, y));
    }

    private static bool AnyInkIn(BitmapFont font, int originX, int originY)
    {
        for (int y = 0; y < font.CellHeight; y++)
        {
            for (int x = 0; x < font.CellWidth; x++)
            {
                if (font.Image.GetPixel(originX + x, originY + y) == font.Ink)
                {
                    return true;
                }
            }
        }

        return false;
    }
}

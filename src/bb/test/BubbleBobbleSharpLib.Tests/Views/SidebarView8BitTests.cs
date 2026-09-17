// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharp.Renditions.EightBit;
using SharpKind.Graphics;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// What the view puts on screen is a fixed pattern of rectangles, so the recording surface proves it
// outright: how many characters are drawn, where they land, and which part of which sheet they come from.
public sealed class SidebarView8BitTests
{
    // Twelve two-row blocks down each edge cover rows 0 to 23, and the last row takes the block's top
    // half on its own: (12 * 2 * 2 + 2) characters an edge, both edges.
    private const int ExpectedCharacters = 100;

    // A level's colour byte: entry 01 is painted colour 6 and entry 10 colour 9.
    private const int Colours = 0x69;

    // A multicolour character: four pixels wide in the sheet, eight on screen, eight rows tall.
    private static readonly Vector2 s_sourceSize = new(4, 8);
    private static readonly Vector2 s_screenSize = new(8, 8);

    [Fact]
    public void DrawsBothEdgesFromTopToBottom()
    {
        RecordingGraphics graphics = Draw(new(3, 7, Colours));

        Assert.Equal(ExpectedCharacters, graphics.ImageParts.Count);
        Assert.All(graphics.ImageParts, x => Assert.Equal(s_screenSize, x.Size));
        Assert.All(graphics.ImageParts, x => Assert.Equal(s_sourceSize, x.SourceSize));

        // The level's leftmost two columns and its rightmost two, and nothing in between.
        Assert.Equal(
            [0f, 8f, 240f, 248f],
            graphics.ImageParts.Select(x => x.Position.X).Distinct().Order());

        // Every row of the screen, none of them twice over.
        Assert.Equal(
            Enumerable.Range(0, 25).Select(x => x * 8f),
            graphics.ImageParts.Select(x => x.Position.Y).Distinct().Order());
    }

    // A design is four characters laid out as a two-by-two block: the first two side by side, the other
    // two on the row below, which is the order draw_border writes them in.
    [Fact]
    public void TakesADesignsFourCharactersFromItsRowOfTheSheet()
    {
        RecordingGraphics graphics = Draw(new(3, 7, Colours));

        Assert.All(graphics.ImageParts, x => Assert.Equal("Sidebars.Recoloured", x.ImageType));

        (string ImageType, Vector2 Position, Vector2 Size, Vector2 SourcePosition, Vector2 SourceSize)[] block =
            [.. graphics.ImageParts.Where(x => x.Position.X < 16 && x.Position.Y < 16)];

        Assert.Equal(
            [new(0, 24), new(4, 24), new(8, 24), new(12, 24)],
            block.Select(x => x.SourcePosition));

        Assert.Equal([new(0, 0), new(8, 0), new(0, 8), new(8, 8)], block.Select(x => x.Position));
    }

    // A level with no design of its own repeats its header tile into all four characters, which the
    // reference reads out of the level tile sheet - a hundred characters, ten to the row.
    [Fact]
    public void RepeatsTheHeaderTileWhenTheLevelHasNoDesign()
    {
        RecordingGraphics graphics = Draw(new(SidebarModel.NoDesign, 23, Colours));

        Assert.Equal(ExpectedCharacters, graphics.ImageParts.Count);
        Assert.All(graphics.ImageParts, x => Assert.Equal("LevelTiles.Recoloured", x.ImageType));
        Assert.All(graphics.ImageParts, x => Assert.Equal(new Vector2(12, 16), x.SourcePosition));
    }

    // The decoration sits inside the level, over the same two background registers the level's own
    // tiles are painted out of, so it wears the level's colours rather than the sheet's own.
    [Fact]
    public void PaintsTheSidebarSheetInTheLevelsColours()
    {
        RecordingGraphics graphics = Draw(new(3, 7, Colours));

        FastBitmap painted = graphics.Image("Sidebars.Recoloured");

        Assert.Equal(TestSurface.Colour(0), painted.GetPixel(0, 0));
        Assert.Equal(TestSurface.Colour(6), painted.GetPixel(1, 0));
        Assert.Equal(TestSurface.Colour(9), painted.GetPixel(2, 0));
    }

    private static RecordingGraphics Draw(SidebarModel model)
    {
        RecordingGraphics graphics = new(320, 200);
        EightBitRendition rendition = new();

        rendition.CreateSidebarView(new TestSurface(graphics, "Sidebars", "LevelTiles")).Draw(model);

        return graphics;
    }
}

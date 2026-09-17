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

// The view's whole job is putting a character where the game left it, so the recording surface proves
// it outright: which sheet each character came from, which part of it, and where it landed.
public sealed class PlayfieldView8BitTests
{
    // A level's colour byte: entry 01 is painted colour 6 and entry 10 colour 9.
    private const int Colours = 0x69;

    // Entry 11 is the cell's colour RAM, which game-loop.s fills the playfield with as $0D - and in
    // multicolour mode only its low three bits are a colour.
    private const int ColourRam = 0x0D & 0x07;

    // A multicolour character: four pixels wide in the sheet, eight on screen, eight rows tall.
    private static readonly Vector2 s_sourceSize = new(4, 8);
    private static readonly Vector2 s_screenSize = new(8, 8);

    [Fact]
    public void DrawsNothingForAnEmptyLevel()
    {
        RecordingGraphics graphics = Draw(new(0, Colours, Empty()));

        Assert.Empty(graphics.ImageParts);
    }

    // The level's own tile is one of a hundred in the tile sheet, ten to the row.
    [Fact]
    public void TakesTheLevelsTileFromTheTileSheet()
    {
        byte[] characters = Empty();
        characters[(3 * PlayfieldModel.Columns) + 5] = PlayfieldModel.LevelTile;

        RecordingGraphics graphics = Draw(new(23, Colours, characters));

        (string sheet, Vector2 position, Vector2 size, Vector2 sourcePosition, Vector2 sourceSize) =
            Assert.Single(graphics.ImageParts);

        Assert.Equal("LevelTiles.Recoloured", sheet);
        Assert.Equal(new(12, 16), sourcePosition);
        Assert.Equal(new(40, 24), position);
        Assert.Equal(s_screenSize, size);
        Assert.Equal(s_sourceSize, sourceSize);
    }

    // The six characters a tile's edges and shadow are drawn with are a row of their own sheet, in the
    // order the screen codes run in.
    [Theory]
    [InlineData(0x0A, 0)]
    [InlineData(0x0C, 8)]
    [InlineData(0x0F, 20)]
    public void TakesATilesEdgesFromTheEdgeSheet(byte character, float sourceX)
    {
        byte[] characters = Empty();
        characters[0] = character;

        RecordingGraphics graphics = Draw(new(0, Colours, characters));

        (string sheet, Vector2 position, Vector2 size, Vector2 sourcePosition, _) =
            Assert.Single(graphics.ImageParts);

        Assert.Equal("TileEdges.Recoloured", sheet);
        Assert.Equal(new(sourceX, 0), sourcePosition);
        Assert.Equal(Vector2.Zero, position);
        Assert.Equal(s_screenSize, size);
    }

    // Every cell of the level, and the last of them at the bottom right of the screen.
    [Fact]
    public void CoversTheWholeOfTheLevel()
    {
        byte[] characters = Empty();
        Array.Fill(characters, PlayfieldModel.LevelTile);

        RecordingGraphics graphics = Draw(new(0, Colours, characters));

        Assert.Equal(PlayfieldModel.Columns * PlayfieldModel.Rows, graphics.ImageParts.Count);
        Assert.Equal(Vector2.Zero, graphics.ImageParts[0].Position);
        Assert.Equal(new(248, 192), graphics.ImageParts[^1].Position);
    }

    // The sheets as exported hold entry numbers wearing the colours of the same number. What the level
    // paints them is its colour byte's two nibbles, and the colour RAM the playfield is filled with.
    [Fact]
    public void PaintsBothSheetsInTheLevelsColours()
    {
        RecordingGraphics graphics = Draw(new(0, Colours, Empty()));

        foreach (string sheet in new[] { "LevelTiles.Recoloured", "TileEdges.Recoloured" })
        {
            FastBitmap painted = graphics.Image(sheet);

            // Entry 00 is the screen's own background, which the sheets carry as a pixel nothing draws.
            Assert.Equal(TestSurface.Colour(0), painted.GetPixel(0, 0));
            Assert.Equal(TestSurface.Colour(6), painted.GetPixel(1, 0));
            Assert.Equal(TestSurface.Colour(9), painted.GetPixel(2, 0));
            Assert.Equal(TestSurface.Colour(ColourRam), painted.GetPixel(3, 0));
        }
    }

    // A level lasts many frames, and the repaint is per level rather than per frame.
    [Fact]
    public void PaintsOnceForAsLongAsTheLevelWearsTheSameColours()
    {
        RecordingGraphics graphics = new(320, 200);
        IView<PlayfieldModel> view = new EightBitRendition().CreatePlayfieldView(
            new TestSurface(graphics, "LevelTiles", "TileEdges"));

        view.Draw(new(0, Colours, Empty()));
        FastBitmap painted = graphics.Image("LevelTiles.Recoloured");

        view.Draw(new(0, Colours, Empty()));
        Assert.Same(painted, graphics.Image("LevelTiles.Recoloured"));

        view.Draw(new(0, 0x12, Empty()));
        Assert.NotSame(painted, graphics.Image("LevelTiles.Recoloured"));
    }

    private static byte[] Empty()
    {
        byte[] characters = new byte[PlayfieldModel.Columns * PlayfieldModel.Rows];
        Array.Fill(characters, PlayfieldModel.Space);

        return characters;
    }

    private static RecordingGraphics Draw(PlayfieldModel model)
    {
        RecordingGraphics graphics = new(320, 200);
        EightBitRendition rendition = new();

        rendition.CreatePlayfieldView(new TestSurface(graphics, "LevelTiles", "TileEdges")).Draw(model);

        return graphics;
    }
}

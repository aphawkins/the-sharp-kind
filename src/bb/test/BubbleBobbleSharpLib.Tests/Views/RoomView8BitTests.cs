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

// A bonus room over the level: each item that is left is the 2x2 block of its art at its cell, painted in the room's colour, the
// clock and the counts are text at their cells, and the results' window is the level's tile round a black inside.
public sealed class RoomView8BitTests
{
    // A level colour byte of $69: entries 01 and 10 are colours 6 and 9.
    private const int Colours = 0x69;
    private const int Art = 0x3F;
    private const int ColourRam = 0x0D;

    [Fact]
    public void DrawsNothingForARoomThatIsNotThere()
    {
        RecordingGraphics graphics = Draw(new([], Art, ColourRam, Colours, 3, [], false));

        Assert.Empty(graphics.ImageParts);
        Assert.Empty(graphics.LeftTexts);
    }

    [Fact]
    public void DrawsEachItemAtItsCellFromItsArt()
    {
        RecordingGraphics graphics = Draw(new([(28, 1), (26, 1), (4, 9)], Art, ColourRam, Colours, 3, [], false));
        BbViewLayout layout = new(320, 200);

        Assert.Equal(3, graphics.ImageParts.Count);
        Assert.All(graphics.ImageParts, part => Assert.Equal("ItemChars.Room.Recoloured", part.ImageType));
        Assert.Equal(
            [layout.Playfield.Cell(28, 1), layout.Playfield.Cell(26, 1), layout.Playfield.Cell(4, 9)],
            graphics.ImageParts.Select(part => part.Position));
        Assert.All(graphics.ImageParts, part => Assert.Equal(new Vector2(Art * 8, 0), part.SourcePosition));
        Assert.All(graphics.ImageParts, part => Assert.Equal(new Vector2(16, 16), part.Size));
    }

    // Entry 11 is the low three bits of the room's colour RAM byte.
    [Fact]
    public void PaintsTheItemsInTheRoomsColour()
    {
        RecordingGraphics graphics = Draw(new([(4, 1)], Art, ColourRam, Colours, 3, [], false));

        FastBitmap painted = graphics.Image("ItemChars.Room.Recoloured");

        Assert.Equal(TestSurface.Colour(6), painted.GetPixel(1, 0));
        Assert.Equal(TestSurface.Colour(9), painted.GetPixel(2, 0));
        Assert.Equal(TestSurface.Colour(ColourRam & 7), painted.GetPixel(3, 0));
    }

    [Fact]
    public void DrawsTheTextAtItsCellsInItsColours()
    {
        RecordingGraphics graphics = Draw(new([], Art, ColourRam, Colours, 3, [new(15, 0, 9, "27"), new(0, 24, 13, "03")], false));
        BbViewLayout layout = new(320, 200);

        Assert.Equal(["27", "03"], graphics.LeftTexts.Select(text => text.Text));
        Assert.Equal([layout.Screen.Cell(15, 0), layout.Screen.Cell(0, 24)], graphics.LeftTexts.Select(text => text.Position));
        Assert.Equal([TestSurface.Colour(9), TestSurface.Colour(13)], graphics.LeftTexts.Select(text => text.Colour));
    }

    // The window is twenty cells by ten: the tile round the edge and a black inside of eighteen by eight.
    [Fact]
    public void DrawsTheResultsWindowAsTheTileRoundABlackInside()
    {
        RecordingGraphics graphics = Draw(new([], Art, ColourRam, Colours, 3, [], true));

        Assert.Equal((RoomModel.BoxWidth * 2) + ((RoomModel.BoxHeight - 2) * 2), graphics.ImageParts.Count);
        Assert.All(graphics.ImageParts, part => Assert.Equal("LevelTiles.Room.Recoloured", part.ImageType));
        Assert.Equal((RoomModel.BoxWidth - 2) * (RoomModel.BoxHeight - 2), graphics.FilledRectangles.Count);
    }

    private static RecordingGraphics Draw(RoomModel model)
    {
        RecordingGraphics graphics = new(320, 200);
        EightBitRendition rendition = new();

        rendition.CreateRoomView(new TestSurface(graphics, "ItemChars", "LevelTiles")).Draw(model);

        return graphics;
    }
}

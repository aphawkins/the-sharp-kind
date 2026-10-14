// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharp.Renditions.EightBit;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// The EXTEND bonus's screens, as the recording surface sees them.
public sealed class ExtendView8BitTests
{
    private const int Colours = 0x69;
    private const int Blocks = 16 * 12;

    [Fact]
    public void ClearedIsTheWholeScreenBlackAndNothingElse()
    {
        RecordingGraphics graphics = Draw(ExtendScreen.Cleared);

        (Vector2 position, float width, float height, _) = Assert.Single(graphics.FilledRectangles);
        Assert.Equal(Vector2.Zero, position);
        Assert.Equal(320, width);
        Assert.Equal(200, height);
        Assert.Empty(graphics.ImageParts);
        Assert.Empty(graphics.LeftTexts);
    }

    // Every block of fruit, the first's top half again in the corner's colour, the box's 56 wall characters, the six
    // letters of the banner, and the player's two lines.
    [Fact]
    public void FruitIsTheBlocksTheBoxTheBannerAndTheText()
    {
        RecordingGraphics graphics = Draw(ExtendScreen.Fruit);

        Assert.Equal(Blocks + 1, graphics.ImageParts.Count(x => x.ImageType.StartsWith("ItemChars.Extend", StringComparison.Ordinal)));
        Assert.Equal(56, graphics.ImageParts.Count(x => x.ImageType == "LevelTiles.Extend.Recoloured"));
        Assert.Equal(
            [new(80, 88), new(96, 88), new(112, 88), new(128, 88), new(144, 88), new(160, 88)],
            graphics.ImageParts.Where(x => x.ImageType == "ExtendChars.Extend.Recoloured").Select(x => x.Position));
        Assert.Equal(["PLAYER 2", "GETS"], graphics.LeftTexts.Select(x => x.Text));
    }

    [Fact]
    public void TextIsThePlayersLinesAlone()
    {
        RecordingGraphics graphics = Draw(ExtendScreen.Text);

        Assert.Empty(graphics.ImageParts);
        Assert.Equal([new(96, 56), new(112, 72)], graphics.LeftTexts.Select(x => x.Position));
    }

    private static RecordingGraphics Draw(ExtendScreen screen)
    {
        RecordingGraphics graphics = new(320, 200);
        EightBitRendition rendition = new();
        byte[] art = new byte[Blocks];
        byte[] colourRam = [.. Enumerable.Repeat((byte)0x0A, Blocks)];

        rendition.CreateExtendView(new TestSurface(graphics, "ItemChars", "LevelTiles", "ExtendChars"))
            .Draw(new(screen, 1, 3, Colours, art, colourRam));

        return graphics;
    }
}

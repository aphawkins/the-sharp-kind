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

// What the view puts on screen is one rectangle per player, so the recording surface proves the
// whole of it: how many are drawn, where they land, which sprite they come from and what colour the
// sheet was painted in.
public sealed class PlayerView8BitTests
{
    // The C64's own sprite origin for a 40-by-25 display, which is the only arithmetic between a
    // player's bytes and the screen: $1805 writes them to the registers untouched.
    private const float OriginX = 24;
    private const float OriginY = 50;

    // Where check_player_state starts a life, and the colours $05C5 gives the two players.
    private const byte SpawnX = 0x2C;
    private const byte SpawnY = 0xDD;
    private const byte PlayerTwoX = 0xEC;
    private const byte PlayerOneColour = 0x05;
    private const byte PlayerTwoColour = 0x03;

    // The last of the level's twenty-five rows, which is the floor the level is drawn inside.
    private const int FloorRow = 24;

    // A multicolour sprite: twelve entry numbers across in the sheet, 24 pixels on screen, 21 rows.
    private static readonly Vector2 s_sourceSize = new(12, 21);
    private static readonly Vector2 s_screenSize = new(24, 21);

    [Fact]
    public void DrawsOneSpriteAtThePositionThePlayersBytesName()
    {
        RecordingGraphics graphics = Draw(Model(state: [0x01, 0x00]));

        (_, Vector2 position, Vector2 size, _, Vector2 sourceSize) = Assert.Single(graphics.ImageParts);

        Assert.Equal(new(SpawnX - OriginX, SpawnY - OriginY), position);
        Assert.Equal(s_screenSize, size);
        Assert.Equal(s_sourceSize, sourceSize);
    }

    // A player at the position a life starts from stands with their feet on the floor: the sprite's
    // bottom edge meets the top of the level's last row, which is row 24 and so begins at pixel 192.
    // That is the one check here that ties the drawing offsets to the game's own numbers rather than
    // to the VIC-II's documentation - $DD and the two origins have to agree for it to land.
    [Fact]
    public void PutsAPlayerStartingALifeOnTheFloor()
    {
        RecordingGraphics graphics = Draw(Model(state: [0x01, 0x00]));

        (_, Vector2 position, Vector2 size, _, _) = Assert.Single(graphics.ImageParts);

        Assert.Equal(FloorRow * 8f, position.Y + size.Y);
    }

    // The sheet is the game's 97 sprites in one row, in the order they sit in memory from $5800, so
    // a sprite's index is its column and nothing has a row to work out.
    [Theory]
    [InlineData(0x00, 0)]
    [InlineData(0x04, 48)]
    [InlineData(0x1F, 372)]
    public void TakesTheSpriteFromItsColumnOfTheSheet(byte frame, float column)
    {
        RecordingGraphics graphics = Draw(Model(state: [0x01, 0x00], frame: [frame, 0]));

        (_, _, _, Vector2 sourcePosition, _) = Assert.Single(graphics.ImageParts);

        Assert.Equal(new(column, 0), sourcePosition);
    }

    // $1817. An empty slot is not drawn - on the C64 what keeps it off the screen is $D015 rather
    // than the sprite's registers, which this port reads as the slot being active.
    [Fact]
    public void DrawsNothingForAnEmptySlot() => Assert.Empty(Draw(Model(state: [0x00, 0x00])).ImageParts);

    // Both players, each from the sheet painted in their own colour. $1805 walks the slots downwards,
    // so player two is written first and player one over it.
    [Fact]
    public void DrawsPlayerOneOverPlayerTwo()
    {
        RecordingGraphics graphics = Draw(Model(state: [0x01, 0x01]));

        Assert.Equal(2, graphics.ImageParts.Count);
        Assert.Equal(
            ["SpritesGame.Colour3", "SpritesGame.Colour5"],
            graphics.ImageParts.Select(x => x.ImageType));
        Assert.Equal(
            [PlayerTwoX - OriginX, SpawnX - OriginX],
            graphics.ImageParts.Select(x => x.Position.X));
    }

    // A multicolour sprite resolves its entries differently from a character: 01 and 11 are the pair
    // of registers every sprite on the screen shares, and only 10 is the sprite's own colour. $44E9
    // and $44EC set that pair to 1 and 2 and nothing writes them again.
    [Fact]
    public void PaintsTheSheetInTheSpritesOwnColour()
    {
        RecordingGraphics graphics = Draw(Model(state: [0x01, 0x00]));

        FastBitmap painted = graphics.Image("SpritesGame.Colour5");

        Assert.Equal(TestSurface.Colour(0), painted.GetPixel(0, 0));
        Assert.Equal(TestSurface.Colour(2), painted.GetPixel(1, 0));
        Assert.Equal(TestSurface.Colour(PlayerOneColour), painted.GetPixel(2, 0));
        Assert.Equal(TestSurface.Colour(1), painted.GetPixel(3, 0));
    }

    private static PlayerModel Model(byte[]? state = null, byte[]? frame = null)
        => new(
            state ?? [0x01, 0x01],
            [SpawnX, PlayerTwoX],
            [SpawnY, SpawnY],
            frame ?? [0x00, 0x04],
            [PlayerOneColour, PlayerTwoColour]);

    private static RecordingGraphics Draw(PlayerModel model)
    {
        RecordingGraphics graphics = new(320, 200);
        EightBitRendition rendition = new();

        rendition.CreatePlayerView(new TestSurface(graphics, "SpritesGame")).Draw(model);

        return graphics;
    }
}

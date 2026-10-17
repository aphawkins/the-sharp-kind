// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

public sealed class BubbleGlideTests
{
    // $E554: a player 58 pixels from the start glides there a point a frame, two an update, in a bubble all the way.
    [Fact]
    public void GlidesAllTheWayToTheStartInABubble()
    {
        EntityTable entities = new();
        entities.Slot(0).State = 1;
        entities.Slot(0).X = 0x66;
        entities.Slot(0).Y = 0xDD;

        BubbleGlide glide = new(entities, LevelScroll.Steps);

        entities.Slot(0).X = 0x2C;

        List<byte> places = [];

        while (!glide.Finished(entities))
        {
            SpriteModel sprites = glide.Sprites(entities);

            Assert.True(sprites.TransitFrame >= 0);
            Assert.True(sprites.IsDrawn(0));
            places.Add(sprites.X(0));
            glide.Pass();
        }

        Assert.Equal(29, places.Count);
        Assert.Equal(0x66, places[0]);
        Assert.Equal(0x2E, places[^1]);
        Assert.Equal(0x2C, glide.Sprites(entities).X(0));
    }

    // $E5C4: a line's last point is its end, and the shorter side is stepped along as the error passes the longer.
    [Theory]
    [InlineData(0x90, 0x60, 0x2C, 0xDD)]
    [InlineData(0x30, 0x40, 0x2C, 0xDD)]
    [InlineData(0x2C, 0xDD, 0xEC, 0xDD)]
    public void EndsWhereTheLineEnds(byte fromX, byte fromY, byte toX, byte toY)
    {
        int length = Math.Max(Math.Abs(fromX - toX), Math.Abs(fromY - toY));

        Assert.Equal((fromX, fromY), BubbleGlide.Point(fromX, fromY, toX, toY, 0));
        Assert.Equal((toX, toY), BubbleGlide.Point(fromX, fromY, toX, toY, length));
        Assert.Equal((toX, toY), BubbleGlide.Point(fromX, fromY, toX, toY, length + 10));
    }

    // A player already at the start, or out of the game, does not hold the level up.
    [Fact]
    public void HasNothingToDoForAPlayerAtTheStart()
    {
        EntityTable entities = new();
        entities.Slot(0).State = 1;
        entities.Slot(0).X = 0x2C;
        entities.Slot(0).Y = 0xDD;

        Assert.True(new BubbleGlide(entities, 0).Finished(entities));
    }
}

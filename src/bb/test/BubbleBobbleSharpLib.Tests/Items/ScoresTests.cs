// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// $7C26, worked by hand.
public sealed class ScoresTests
{
    [Fact]
    public void AddsInDecimalAtTheByteNamed()
    {
        Scores scores = new();

        scores.Add(2, 0x50);
        scores.Add(1, 0x01);

        Assert.Equal([0x00, 0x01, 0x50, 0x00, 0x00, 0x00], scores.Bytes.ToArray());
    }

    // $7C2F. A carry adds one to the byte before.
    [Fact]
    public void CarriesTowardsTheFirstByte()
    {
        Scores scores = new();
        scores.Add(2, 0x99);
        scores.Add(1, 0x99);

        scores.Add(2, 0x01);

        Assert.Equal([0x01, 0x00, 0x00, 0x00, 0x00, 0x00], scores.Bytes.ToArray());
    }

    // $7C34's `bpl`. Player two's carry runs on into player one's bytes, as on the 6502.
    [Fact]
    public void PlayerTwosCarryIsNotStoppedAtTheirFirstByte()
    {
        Scores scores = new();
        scores.Add(3, 0x99);

        scores.Add(3, 0x01);

        Assert.Equal([0x00, 0x00, 0x01, 0x00, 0x00, 0x00], scores.Bytes.ToArray());
    }

    [Fact]
    public void NamesEachPlayersLowestByte()
    {
        Assert.Equal(2, Scores.Last(0));
        Assert.Equal(5, Scores.Last(1));
    }
}

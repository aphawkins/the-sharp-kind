// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// $2B31, $06C6, $1578 and $2CB7, worked by hand off the reference. The RNG is held at zero, so every
// draw is 0: the food is $58 exactly, and the special item is the rare $1E.
public sealed class LevelItemsTests
{
    // Level 1's cells: food at 9,7 and the special item at 21,7.
    private static readonly Level s_level = new()
    {
        Number = 1,
        FoodDrop = new() { X = 9, Y = 7 },
        PowerupSpawn = new() { X = 21, Y = 7 },
    };

    // $2B41 to $2C9E. Both hidden, with three seconds and one to wait.
    [Fact]
    public void SetsBothItemsUpHidden()
    {
        Rig rig = new();
        rig.Items.FoodBase = 0x10;

        rig.Items.Setup(s_level, 0);

        Assert.Equal([0x90, 0x9E], rig.Items.Type.ToArray());
        Assert.Equal([0x5C, 0xBC], rig.Items.X.ToArray());
        Assert.Equal([0x65, 0x65], rig.Items.Y.ToArray());
        Assert.Equal([0x03, 0x01], rig.Items.Timer.ToArray());
        Assert.Equal(0, rig.Items.FoodBase);
    }

    // $2BB7. The best food is $2E.
    [Fact]
    public void CapsTheFood()
    {
        Rig rig = new();
        rig.Items.FoodBase = 0x40;

        rig.Items.Setup(s_level, 0);

        Assert.Equal(0xAE, rig.Items.Type[0]);
    }

    // $2C91. Past the byte, the X add's carry lands in Y.
    [Fact]
    public void TheXCarryMovesTheItemDownAPixel()
    {
        Rig rig = new();
        Level level = new() { FoodDrop = new() { X = 30, Y = 7 }, PowerupSpawn = new() { X = 1, Y = 1 } };

        rig.Items.Setup(level, 0);

        Assert.Equal(0x04, rig.Items.X[0]);
        Assert.Equal(0x66, rig.Items.Y[0]);
    }

    // $2BDB. A player who has not died gets $20 every ten levels, from $12.
    [Theory]
    [InlineData(0x12, 0x13, 0xA0, 0x1C)]
    [InlineData(0x12, 0x12, 0x9E, 0x12)]
    [InlineData(0x30, 0x31, 0xA1, 0x4E)]
    public void AMilestoneLevelHasItsOwnItem(byte milestone, byte number, byte type, byte next)
    {
        Rig rig = new();
        rig.Items.Undying = 0x01;
        rig.Items.Milestone = milestone;

        rig.Items.Setup(s_level, number);

        Assert.Equal(type, rig.Items.Type[1]);
        Assert.Equal(next, rig.Items.Milestone);
    }

    // $2B5F, $2BC2, $2BCD, $2C37 and $2C40. The food $10 and the special item $1E take their art and
    // colour from $A892, $A8E4, $A8C1 and $A913, and are drawn at their level cells.
    [Fact]
    public void EachItemTakesItsArtAndColour()
    {
        Rig rig = new();
        rig.Items.FoodBase = 0x10;

        rig.Items.Setup(s_level, 0);

        Assert.Equal([0x26, 0x1A], rig.Items.Art.ToArray());
        Assert.Equal([0x0B, 0x0C], rig.Items.Colour.ToArray());
        Assert.Equal([9, 21], rig.Items.Column.ToArray());
        Assert.Equal([7, 7], rig.Items.Row.ToArray());
    }

    // $2CB7. The special item $18 flashes while it shows, and not while it is hidden.
    [Theory]
    [InlineData(0x18, 0x09)]
    [InlineData(0x98, 0x0C)]
    public void TheFlashingItemFlipsItsColour(byte type, byte colour)
    {
        Rig rig = new();
        rig.Items.Setup(s_level, 0);
        rig.Items.Type[1] = type;

        rig.Items.Collect(0);

        Assert.Equal(colour, rig.Items.Colour[1]);
    }

    [Fact]
    public void ThrowsForTheBonusLevel()
    {
        Rig rig = new();

        Assert.Throws<NotSupportedException>(() => rig.Items.Setup(s_level, 0x63));
    }

    // $06C6.
    [Fact]
    public void TicksBothTimers()
    {
        Rig rig = new();
        rig.Items.Timer[0] = 0x05;
        rig.Items.Timer[1] = 0x00;

        rig.Items.Tick();

        Assert.Equal([0x04, 0xFF], rig.Items.Timer.ToArray());
    }

    // $1578. Out of time, a hidden item appears for eleven seconds.
    [Fact]
    public void AnItemAppearsWhenItsTimerRunsOut()
    {
        Rig rig = new();
        rig.Items.Type[1] = 0x85;
        rig.Items.Timer[1] = 0xFF;

        rig.Items.Update();

        Assert.Equal(0x05, rig.Items.Type[1]);
        Assert.Equal(0x0B, rig.Items.Timer[1]);
    }

    // $158D. Out of time again, it goes - and the `rts` leaves the food for the next pass.
    [Fact]
    public void AnItemGoesAndTheOtherWaits()
    {
        Rig rig = new();
        rig.Items.Type[1] = 0x05;
        rig.Items.Timer[1] = 0xFF;
        rig.Items.Type[0] = 0x90;
        rig.Items.Timer[0] = 0xFF;

        rig.Items.Update();

        Assert.Equal(0xFF, rig.Items.Type[1]);
        Assert.Equal(0x90, rig.Items.Type[0]);
        Assert.Equal(0xFF, rig.Items.Timer[0]);
    }

    // $2D13. Food below $0F scores in the lowest byte, and from $0F in the one above.
    [Theory]
    [InlineData(0x08, 2, 0x50)]
    [InlineData(0x10, 1, 0x01)]
    public void TakingTheFoodScores(byte type, int index, byte score)
    {
        Rig rig = new();
        Show(rig, 0, type);
        Player(rig, 0, 0x01);

        rig.Items.Collect(0);

        Assert.Equal(score, rig.Scores.Bytes[index]);
        Assert.Equal(0xFF, rig.Items.Timer[0]);
        Assert.Equal(type, rig.Items.Type[0]);
    }

    // $2D20 and $2D40. The special item scores, and its effect runs for the player who took it:
    // $03 is a candy and $0C a ring.
    [Fact]
    public void TakingTheSpecialItemScoresAndActs()
    {
        Rig rig = new();
        Show(rig, 1, 0x03);
        Player(rig, 1, 0x01);

        rig.Items.Collect(0);

        Assert.Equal(0x10, rig.Scores.Bytes[5]);
        Assert.Equal(0x03, rig.Players.BlowReload[1]);

        Show(rig, 1, 0x0C);
        rig.Items.Collect(0);

        Assert.Equal(0x20, rig.Scores.Bytes[5]);
        Assert.Equal(0xFF, rig.Players.DriftRing[1]);
    }

    // $2D25. From $18 a byte higher.
    [Fact]
    public void AHighSpecialItemScoresAByteHigher()
    {
        Rig rig = new();
        Show(rig, 1, 0x1F);
        Player(rig, 0, 0x01);

        Assert.Throws<NotSupportedException>(() => rig.Items.Collect(0));
        Assert.Equal(0x08, rig.Scores.Bytes[1]);
    }

    // $2CC5. A hidden item, or a player out of reach, takes nothing. States below $0E may take it, and
    // $10 and $18; a dying player, $0E, may not.
    [Theory]
    [InlineData(0x08, 0x01, 0x00, true)]
    [InlineData(0x88, 0x01, 0x00, false)]
    [InlineData(0x08, 0x01, 0x10, false)]
    [InlineData(0x08, 0x0D, 0x00, true)]
    [InlineData(0x08, 0x0E, 0x00, false)]
    [InlineData(0x08, 0x10, 0x00, true)]
    [InlineData(0x08, 0x18, 0x00, true)]
    public void OnlyAPlayerThereAndAbleTakesIt(byte type, byte state, byte offset, bool taken)
    {
        Rig rig = new();
        Show(rig, 0, type);
        Player(rig, 0, state);
        rig.Entities.X[0] = (byte)(rig.Entities.X[0] + offset);

        rig.Items.Collect(0);

        Assert.Equal(taken, rig.Scores.Bytes[2] != 0);
    }

    // $2CB7's loop. The item stays until $1578, so both players take it.
    [Fact]
    public void BothPlayersCanTakeTheSameItem()
    {
        Rig rig = new();
        Show(rig, 0, 0x08);
        Player(rig, 0, 0x01);
        Player(rig, 1, 0x01);

        rig.Items.Collect(0);

        Assert.Equal(0x50, rig.Scores.Bytes[2]);
        Assert.Equal(0x50, rig.Scores.Bytes[5]);
    }

    private static void Show(Rig rig, int item, byte type)
    {
        rig.Items.Type[item] = type;
        rig.Items.X[item] = 0x80;
        rig.Items.Y[item] = 0x80;
        rig.Items.Timer[item] = 0x05;
    }

    private static void Player(Rig rig, int player, byte state)
    {
        rig.Entities.State[player] = state;
        rig.Entities.X[player] = 0x8F;
        rig.Entities.Y[player] = 0x71;
    }

    private sealed class Rig
    {
        internal Rig() => Items = new(
            Entities,
            Scores,
            new(Players, Entities),
            new(new FakeRandomSource()));

        internal EntityTable Entities { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal Scores Scores { get; } = new();

        internal LevelItems Items { get; }
    }
}

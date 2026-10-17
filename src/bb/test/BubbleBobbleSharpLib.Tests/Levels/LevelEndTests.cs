// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// $15E4 and $15FA, worked by hand off the reference. The ending itself is proved against VICE.
public sealed class LevelEndTests
{
    // $05C5: a level starts not over.
    [Fact]
    public void ALevelStartsNotOver()
    {
        EndRig rig = Arranged(0xFF);
        rig.Expire();

        Assert.Equal(1, rig.End.Complete);

        rig.End.Begin();

        Assert.Equal(0, rig.End.Complete);
    }

    // $15FA-$1620: the players' flash goes back to what a bonus round saved, and their colours to their own.
    [Fact]
    public void APlayerFlashingIsPutBackToPlay()
    {
        EndRig rig = Arranged(0xFF);
        rig.Entities.Slot(1).State = PlayerRespawn.RespawningState;
        rig.Entities.Slot(0).State = PlayerFrame.PlayingState;
        rig.Entities.Slot(1).Colour = 0x0E;
        rig.Entities.Slot(1).FlashTimer = 0x33;
        rig.End.SavedFlash[1] = 0x04;

        rig.Expire();

        Assert.Equal(PlayerFrame.PlayingState, rig.Entities.Slot(1).State);
        Assert.Equal(0x03, rig.Entities.Slot(1).Colour);
        Assert.Equal(0x04, rig.Entities.Slot(1).FlashTimer);
        Assert.Equal(1, rig.End.Complete);
    }

    // $1607-$1613: a player dying or waiting to respawn holds the end up, and the ones before it are done.
    [Theory]
    [InlineData(PlayerFrame.DyingState)]
    [InlineData(PlayerFrame.DeadState)]
    [InlineData(0x18)]
    public void APlayerInAnyOtherStateHoldsTheEndUp(byte state)
    {
        EndRig rig = Arranged(0xFF);
        rig.Entities.Slot(1).State = PlayerRespawn.RespawningState;
        rig.Entities.Slot(0).State = state;

        rig.Expire();

        Assert.Equal(0, rig.End.Complete);
        Assert.Equal(PlayerFrame.PlayingState, rig.Entities.Slot(1).State);
        Assert.Equal(state, rig.Entities.Slot(0).State);
    }

    // $15E8-$15F4, $3AB8: the first time the clock runs out every enemy still to be caught is angry, the level
    // is in its hurry-up, and there are ten seconds more.
    [Fact]
    public void TheFirstTimeTheClockRunsOutIsTheHurryUp()
    {
        EndRig rig = Arranged(0);
        rig.Entities.Slot(2).State = 0x02;
        rig.Entities.Slot(3).State = 0x0B;
        rig.Entities.Slot(4).State = 0x0A;

        rig.Expire();

        Assert.Equal(1, rig.Timer.Hurry);
        Assert.Equal(0x0A, rig.Timer.Seconds);
        Assert.Equal(0xFF, rig.Entities.Slot(2).FlashTimer);
        Assert.Equal(0x00, rig.Entities.Slot(3).FlashTimer);
        Assert.Equal(0xFF, rig.Entities.Slot(4).FlashTimer);
        Assert.Equal(0, rig.End.Complete);
    }

    // $1621-$1693: the second time brings a Baron for each player in the game, in the slot of the player's number,
    // at a place chosen by four rolls (player 2's column and row, then player 1's), and stops the clock.
    [Theory]
    [InlineData(new byte[] { 0xA2, 0x74, 0x0B, 0xD8 }, 0x1B, 0x05, 0x02, 0x05)]
    [InlineData(new byte[] { 0x3D, 0x85, 0x2A, 0x97 }, 0x02, 0x19, 0x1B, 0x19)]
    public void TheSecondTimeBringsTheBarons(byte[] rolls, int column0, int row0, int column1, int row1)
    {
        ArgumentNullException.ThrowIfNull(rolls);

        EndRig rig = new(ScriptedRandomSource.For(0x8D, 0x18, rolls));
        rig.Random.Low = 0x8D;
        rig.Random.High = 0x18;
        rig.Timer.Seconds = 0;
        rig.Timer.Hurry = 1;
        rig.Entities.Slot(0).State = PlayerFrame.PlayingState;
        rig.Entities.Slot(1).State = PlayerFrame.PlayingState;
        rig.Objects.Slot(0).Variant = 0x11;
        rig.Objects.Slot(1).EnemyType = 0x14;

        rig.Expire();

        Assert.Equal(0x2E, rig.Objects.Slot(0).Type);
        Assert.Equal(0x30, rig.Objects.Slot(1).Type);
        Assert.Equal(column0, rig.Objects.Slot(0).Column);
        Assert.Equal(row0, rig.Objects.Slot(0).Row);
        Assert.Equal(column1, rig.Objects.Slot(1).Column);
        Assert.Equal(row1, rig.Objects.Slot(1).Row);
        Assert.Equal(0, rig.Objects.Slot(0).Variant);
        Assert.Equal(0, rig.Objects.Slot(1).EnemyType);
        Assert.Equal(0xFF, rig.Items.FoodBase);
        Assert.Equal(LevelTimer.Stopped, rig.Timer.Frames);
        Assert.Equal(0, rig.Timer.Hurry);
        Assert.Equal(0x1E, rig.Timer.Seconds);
    }

    // $1637-$1660: a player not in the game has no Baron, though the slot still takes the place.
    [Fact]
    public void APlayerNotInTheGameHasNoBaron()
    {
        EndRig rig = new(ScriptedRandomSource.For(0, 0, [0xAF, 0x5F, 0xC1, 0x22]));
        rig.Timer.Seconds = 0;
        rig.Timer.Hurry = 1;
        rig.Entities.Slot(0).State = 0;
        rig.Entities.Slot(1).State = PlayerFrame.PlayingState;

        rig.Expire();

        Assert.Equal(ObjectTable.FreeType, rig.Objects.Slot(0).Type);
        Assert.Equal(0x30, rig.Objects.Slot(1).Type);
    }

    // $1AE8-$1B18: a bubble in either slot is popped, and the game waits for it to go, before the Baron comes.
    [Fact]
    public void ABubbleInABaronsSlotPopsFirst()
    {
        EndRig rig = new(ScriptedRandomSource.For(0, 0, [0x01, 0x01, 0x01, 0x01]));
        rig.Timer.Seconds = 0;
        rig.Timer.Hurry = 1;
        rig.Entities.Slot(0).State = PlayerFrame.PlayingState;
        rig.Objects.Slot(0).Type = 0x3A;
        rig.Objects.Slot(0).Column = 6;
        rig.Objects.Slot(0).Row = 7;

        rig.Expire();

        Assert.Equal(0x2E, rig.Objects.Slot(0).Type);
    }

    private static EndRig Arranged(byte hurry)
    {
        EndRig rig = new();
        rig.Timer.Seconds = 0;
        rig.Timer.Hurry = hurry;
        rig.Entities.Slot(0).State = PlayerFrame.PlayingState;
        return rig;
    }
}

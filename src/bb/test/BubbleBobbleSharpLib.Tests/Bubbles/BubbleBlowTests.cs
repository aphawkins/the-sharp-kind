// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Bubbles;

// $22E8 and $2301: the button, the six frames after it, and the bubble that comes out of the
// middle of them.
//
// The player stands at $44/$55 throughout, which is a column boundary on a row boundary - the same
// position PlayerFrameTests uses, so that a number appearing in both files means the same thing.
// From there the bubble's own arithmetic is checkable by hand: $44 less $14 is $30, which is column
// six exactly, and $55 less $15 is $40, which is row eight exactly.
public sealed class BubbleBlowTests
{
    private const byte StandX = 0x44;
    private const byte StandY = 0x55;

    // $2321 counts down from seventeen, so an empty table hands out its last slot first.
    private const int FirstSlot = ObjectTable.Capacity - 1;

    // $8520 as the movers leave it. Facing right is the pair 0 and 1, facing left the pair 4 and 5.
    private const byte FacingRight = 0x00;
    private const byte FacingLeft = 0x04;

    // $22EE. Pressing fire starts a clock and does nothing else - no bubble on this frame.
    [Fact]
    public void StartsTheTimerAndLatchesTheFacing()
    {
        (PlayerTable players, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(FacingRight);

        blow.Blow(0);

        Assert.Equal(BubbleBlow.BlowFrames, entities.Slot(0).BubbleTimer);
        Assert.Equal(0x00, players.BlowFacing[0]);
        Assert.Equal(ObjectTable.FreeType, objects.Slot(FirstSlot).Type);
    }

    // $22F4. Bit 2 of the sprite halved, which is the 2 that $230E adds to the table.
    [Fact]
    public void LatchesTwoWhenFacingLeft()
    {
        (PlayerTable players, _, _, BubbleBlow blow) = Standing(FacingLeft);

        blow.Blow(0);

        Assert.Equal(0x02, players.BlowFacing[0]);
    }

    // $22E8. The reload is the first thing asked, and it blocks the whole routine - the timer is
    // not even started, so a player who presses fire too soon gets no animation either.
    [Fact]
    public void RefusesToBlowWhileReloading()
    {
        (PlayerTable players, EntityTable entities, _, BubbleBlow blow) = Standing(FacingRight);
        players.Reload[0] = 0x01;

        blow.Blow(0);

        Assert.Equal(0xFF, entities.Slot(0).BubbleTimer);
    }

    // $22ED. A blow already running owns the byte, so holding fire does not restart it part way
    // through and stretch the animation out.
    [Fact]
    public void RefusesToRestartABlowAlreadyRunning()
    {
        (_, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(FacingRight);

        blow.Blow(0);
        entities.Slot(0).BubbleTimer = 0x02;
        blow.Blow(0);

        Assert.Equal(0x02, entities.Slot(0).BubbleTimer);
        Assert.Equal(ObjectTable.FreeType, objects.Slot(FirstSlot).Type);
    }

    // $ACC4 walked from six down to one, then $2303 putting the sprite back. Asserted frame by
    // frame, because the table is read at the timer's own value and an index one out gives a
    // sequence that is still plausible.
    [Fact]
    public void RunsTheSixFramesAndPutsTheSpriteBack()
    {
        (_, EntityTable entities, _, BubbleBlow blow) = Standing(FacingRight);
        blow.Blow(0);

        byte[] expected = [0x08, 0x08, 0x09, 0x09, 0x08, 0x08, 0x00];
        byte[] actual = new byte[expected.Length];

        for (int tick = 0; tick < expected.Length; tick++)
        {
            Step(blow, entities);
            actual[tick] = entities.Slot(0).Frame;
        }

        Assert.Equal(expected, actual);
        Assert.Equal(0xFF, entities.Slot(0).BubbleTimer);
    }

    // The same six frames facing left, which is the whole table plus two. It is asserted separately
    // because the two are added rather than the table being mirrored.
    [Fact]
    public void RunsTheSixFramesFacingLeft()
    {
        (_, EntityTable entities, _, BubbleBlow blow) = Standing(FacingLeft);
        blow.Blow(0);

        byte[] expected = [0x0A, 0x0A, 0x0B, 0x0B, 0x0A, 0x0A, 0x04];
        byte[] actual = new byte[expected.Length];

        for (int tick = 0; tick < expected.Length; tick++)
        {
            Step(blow, entities);
            actual[tick] = entities.Slot(0).Frame;
        }

        Assert.Equal(expected, actual);
    }

    // $231B. The bubble is made on one frame out of the six and not before it - three frames after
    // the button, which is long enough to see.
    [Fact]
    public void MakesTheBubbleOnTheThirdFrameAndNotBefore()
    {
        (_, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(FacingRight);
        blow.Blow(0);

        Step(blow, entities);
        Step(blow, entities);

        Assert.Equal(ObjectTable.FreeType, objects.Slot(FirstSlot).Type);

        Step(blow, entities);

        Assert.Equal(BubbleBlow.BubbleType, objects.Slot(FirstSlot).Type);
    }

    // And only one bubble out of one blow, however many frames are left to run.
    [Fact]
    public void MakesOneBubblePerBlow()
    {
        (_, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(FacingRight);
        blow.Blow(0);

        for (int tick = 0; tick < 7; tick++)
        {
            Step(blow, entities);
        }

        Assert.Equal(BubbleBlow.BubbleType, objects.Slot(FirstSlot).Type);
        Assert.Equal(ObjectTable.FreeType, objects.Slot(FirstSlot - 1).Type);
    }

    // $232A and $2341. The pixel position is copied straight across and the cell worked out from
    // it, and the cell is a plain cell number rather than PlayerCell's index into the map - $232A
    // does its own arithmetic and none of PlayerCell's biases are in it.
    [Fact]
    public void PlacesTheBubbleFromThePlayersOwnPosition()
    {
        (_, _, ObjectTable objects, _) = Blown(FacingRight);

        // $44 is column six, and facing right moves the bubble on by one.
        Assert.Equal(0x4C, objects.Slot(FirstSlot).X);
        Assert.Equal(7, objects.Slot(FirstSlot).Column);
        Assert.Equal(0x00, objects.Slot(FirstSlot).SubX);

        // $55 is row eight, and a standing player does not get the row below.
        Assert.Equal(StandY, objects.Slot(FirstSlot).Y);
        Assert.Equal(8, objects.Slot(FirstSlot).Row);
        Assert.Equal(0x00, objects.Slot(FirstSlot).SubY);

        Assert.Equal(0x00, objects.Slot(FirstSlot).Direction);
    }

    // $2397. Facing left there is no shift at all, because the sprite's origin is already where the
    // player's mouth is. It is the asymmetry a mirrored translation would smooth away.
    [Fact]
    public void DoesNotShiftTheBubbleWhenFacingLeft()
    {
        (_, _, ObjectTable objects, _) = Blown(FacingLeft);

        Assert.Equal(StandX, objects.Slot(FirstSlot).X);
        Assert.Equal(6, objects.Slot(FirstSlot).Column);
        Assert.Equal(0x80, objects.Slot(FirstSlot).Direction);
    }

    // $2352 and $2364. The seven bytes $2321's slot is filled with, none of which Phase 5 reads
    // back yet - they are asserted so that the routines that do read them find what the 6502 left.
    [Fact]
    public void FillsTheSlotsOtherBytes()
    {
        (_, _, ObjectTable objects, _) = Blown(FacingRight);

        Assert.Equal(0x7D, objects.Slot(FirstSlot).Flags);
        Assert.Equal(0x7D, objects.Slot(FirstSlot).Behaviour);
        Assert.Equal(0x88, objects.Slot(FirstSlot).State);
        Assert.Equal(0x04, objects.Slot(FirstSlot).Variant);
        Assert.Equal(0x04, objects.Slot(FirstSlot).EnemyType);
    }

    // $2399. Both halves of the test have to hold. Off the ground and low in the cell, the bubble
    // goes into the row below - so a player blowing at the bottom of their cell does not put the
    // bubble through the floor they are about to land on.
    [Fact]
    public void BlowsIntoTheRowBelowWhenAirborneAndLowInTheCell()
    {
        (PlayerTable players, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(FacingRight);

        // $59 less $15 is $44: row eight still, but four pixels down it.
        entities.Slot(0).Y = 0x59;
        entities.Slot(0).RiseCounter = 0x05;

        Run(blow, entities, players);

        Assert.Equal(9, objects.Slot(FirstSlot).Row);
        Assert.Equal(0x61, objects.Slot(FirstSlot).Y);
    }

    // The same cell, standing. $87A0 negative is the whole difference, and without the test on it
    // every bubble blown low in a cell would be a row out.
    [Fact]
    public void DoesNotBlowIntoTheRowBelowWhileStanding()
    {
        (PlayerTable players, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(FacingRight);
        entities.Slot(0).Y = 0x59;

        Run(blow, entities, players);

        Assert.Equal(8, objects.Slot(FirstSlot).Row);
        Assert.Equal(0x59, objects.Slot(FirstSlot).Y);
    }

    // And airborne but high in the cell, which is the other half of the same test.
    [Fact]
    public void DoesNotBlowIntoTheRowBelowHighInTheCell()
    {
        (PlayerTable players, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(FacingRight);
        entities.Slot(0).RiseCounter = 0x05;

        Run(blow, entities, players);

        Assert.Equal(8, objects.Slot(FirstSlot).Row);
        Assert.Equal(StandY, objects.Slot(FirstSlot).Y);
    }

    // $2349. A player above the top of the playfield borrows from the subtraction and the spawn is
    // abandoned half done. The slot keeps the bytes already written and stays free, which is what
    // the 6502 leaves behind - nothing has put a type in it.
    [Fact]
    public void AbandonsTheSpawnAboveTheTopOfThePlayfield()
    {
        (PlayerTable players, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(FacingRight);
        entities.Slot(0).Y = 0x10;

        Run(blow, entities, players);

        Assert.Equal(ObjectTable.FreeType, objects.Slot(FirstSlot).Type);
        Assert.Equal(0x10, objects.Slot(FirstSlot).Y);
        Assert.Equal(StandX, objects.Slot(FirstSlot).X);
        Assert.Equal(0x00, players.Reload[0]);
    }

    // $2329. Eighteen bubbles already in the air and the nineteenth is simply not made - and the
    // player is not charged a reload for it either, so they can try again next frame.
    [Fact]
    public void MakesNoBubbleWithNoFreeSlot()
    {
        (PlayerTable players, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(FacingRight);
        objects.Type.Fill(BubbleBlow.BubbleType);

        Run(blow, entities, players);

        Assert.Equal(0x00, players.Reload[0]);
    }

    // $2364 and $0A28. Eight frames, counted down one a frame, and the count starts at the bubble
    // rather than at the button.
    [Fact]
    public void ChargesEightFramesOfReloadAndCountsThemDown()
    {
        (PlayerTable players, EntityTable entities, _, BubbleBlow blow) = Standing(FacingRight);

        Run(blow, entities, players);

        Assert.Equal(0x08, players.Reload[0]);

        for (int tick = 0; tick < 8; tick++)
        {
            blow.Tick();
        }

        Assert.Equal(0x00, players.Reload[0]);
    }

    // $0A30. A reload already at zero stays there rather than wrapping to $FF, which would lock the
    // player out for the rest of the level.
    [Fact]
    public void DoesNotTickAReloadBelowZero()
    {
        (PlayerTable players, _, _, BubbleBlow blow) = Standing(FacingRight);

        blow.Tick();

        Assert.Equal(0x00, players.Reload[0]);
    }

    // The two players' bytes are their own. One blowing leaves the other able to.
    [Fact]
    public void KeepsTheTwoPlayersApart()
    {
        (PlayerTable players, EntityTable entities, _, BubbleBlow blow) = Standing(FacingRight);
        entities.Slot(1).BubbleTimer = 0xFF;

        Run(blow, entities, players);

        Assert.Equal(0x08, players.Reload[0]);
        Assert.Equal(0x00, players.Reload[1]);
        Assert.Equal(0xFF, entities.Slot(1).BubbleTimer);
    }

    // $23B8-$23D6: a player with thunder shots left blows one instead, which goes the way they face.
    [Theory]
    [InlineData(FacingRight, 2)]
    [InlineData(FacingLeft, 0)]
    public void ABlowWithThunderShotsLeftMakesAShotFacingTheWayThePlayerFaces(byte facing, byte subX)
    {
        (PlayerTable players, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(facing);
        players.Shots[0] = 0x12;

        Run(blow, entities, players);

        Assert.Equal(0x11, players.Shots[0]);
        Assert.Equal(BubbleBlow.ThunderType, objects.Slot(FirstSlot).Type);
        Assert.Equal(subX, objects.Slot(FirstSlot).SubX);
        Assert.Equal(0, objects.Slot(FirstSlot).State);
    }

    // $23B8: $FF is none, and the last shot ($00) is still a shot.
    [Theory]
    [InlineData(0xFF, BubbleBlow.BubbleType)]
    [InlineData(0x00, BubbleBlow.ThunderType)]
    public void OnlyAPlayerWithAShotBlowsThunder(byte shots, byte type)
    {
        (PlayerTable players, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(FacingRight);
        players.Shots[0] = shots;

        Run(blow, entities, players);

        Assert.Equal(type, objects.Slot(FirstSlot).Type);
    }

    // The blow, run as far as the frame the bubble is made on.
    private static void Run(BubbleBlow blow, EntityTable entities, PlayerTable players)
    {
        blow.Blow(0);

        while (entities.Slot(0).BubbleTimer >= BubbleBlow.SpawnTimer)
        {
            Step(blow, entities, players);
        }
    }

    // $2165. One frame of the blow, with the timer as it stands before the frame - which is what
    // $2162 hands $2301.
    private static void Step(BubbleBlow blow, EntityTable entities, PlayerTable? players = null)
    {
        byte x = players is null ? StandX : entities.Slot(0).X;
        byte y = players is null ? StandY : entities.Slot(0).Y;

        blow.Step(0, entities.Slot(0).BubbleTimer, PlayerCell.Of(x, y));
    }

    // A blow run to the bubble, for the cases that are about where the bubble landed.
    private static (PlayerTable Players, EntityTable Entities, ObjectTable Objects, BubbleBlow Blow) Blown(byte facing)
    {
        (PlayerTable players, EntityTable entities, ObjectTable objects, BubbleBlow blow) = Standing(facing);

        Run(blow, entities, players);

        return (players, entities, objects, blow);
    }

    // One player, standing where PlayerFrameTests stands them, with the four counters a level start
    // puts back to $FF.
    private static (PlayerTable Players, EntityTable Entities, ObjectTable Objects, BubbleBlow Blow) Standing(byte facing)
    {
        PlayerTable players = new();
        EntityTable entities = new();
        ObjectTable objects = new();

        entities.Slot(0).State = PlayerFrame.PlayingState;
        entities.Slot(0).X = StandX;
        entities.Slot(0).Y = StandY;

        entities.Slot(0).Frame = facing;
        entities.Slot(0).RiseCounter = 0xFF;
        entities.Slot(0).FallCounter = 0xFF;
        entities.Slot(0).GroundState = 0xFF;
        entities.Slot(0).BubbleTimer = 0xFF;

        return (players, entities, objects, new BubbleBlow(players, entities, objects, new(players, new())));
    }
}

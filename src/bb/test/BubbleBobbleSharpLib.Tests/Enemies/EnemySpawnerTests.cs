// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// $1E2E and $39D2, worked by hand off the reference. The random source is held fixed so the one draw
// each enemy takes is known.
public sealed class EnemySpawnerTests
{
    // Level 1's first enemy: class 0, column 15, row 0, delay 10, facing and moving left.
    [Fact]
    public void PutsLevelOnesFirstEnemyInSlotTwo()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(15, 0, 0, delay: 10, faceLeft: true, moveLeft: true)]);

        Assert.Equal(0x02, entities.Slot(2).State);
        Assert.Equal(0x8C, entities.Slot(2).X);
        Assert.Equal(0x15, entities.Slot(2).Y);
        Assert.Equal(0x01, entities.Slot(2).Heading);
        Assert.Equal(0x04, entities.Slot(2).Frame);
        Assert.Equal(0x14, entities.Slot(2).HoldTimer);
        Assert.Equal(0x04, entities.Slot(2).FrameCount);
        Assert.Equal(0x03, entities.Slot(2).FrameMask);
        Assert.Equal(0x73, entities.Slot(2).SpriteBase);
        Assert.Equal(0x0C, entities.Slot(2).Colour);
        Assert.Equal(0x1E, entities.Slot(2).TurnTimer);
        Assert.Equal(0x1E, entities.Slot(2).TurnInterval);
    }

    // Class 3 is state 5, the one DiagonalMover answers.
    [Fact]
    public void GivesClassThreeStateFive()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(4, 4, 3)]);

        Assert.Equal(0x05, entities.Slot(2).State);
    }

    // State 9 reads each table's index 9, which is the next table's second byte. The frame count is
    // $AB81's second byte, $00, so the mask is $FF.
    [Fact]
    public void ReadsPastEachTableForClassSeven()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(4, 4, 7, faceLeft: true)]);

        Assert.Equal(0x09, entities.Slot(2).State);
        Assert.Equal(0x0F, entities.Slot(2).Colour);
        Assert.Equal(0xBB, entities.Slot(2).SpriteBase);
        Assert.Equal(0x14, entities.Slot(2).TurnTimer);
        Assert.Equal(0x00, entities.Slot(2).FrameCount);
        Assert.Equal(0xFF, entities.Slot(2).FrameMask);
        Assert.Equal(0x00, entities.Slot(2).Frame);
    }

    [Fact]
    public void ReadsPastEachTableForClassSix()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(4, 4, 6, faceLeft: true)]);

        Assert.Equal(0x08, entities.Slot(2).State);
        Assert.Equal(0x03, entities.Slot(2).Colour);
        Assert.Equal(0xB3, entities.Slot(2).SpriteBase);
        Assert.Equal(0x0A, entities.Slot(2).TurnTimer);
        Assert.Equal(0x02, entities.Slot(2).FrameCount);
        Assert.Equal(0x01, entities.Slot(2).FrameMask);
        Assert.Equal(0x02, entities.Slot(2).Frame);
    }

    // $3A21's `beq` stores the zero the `and #$40` left.
    [Fact]
    public void StartsAThingFacingRightOnFrameZero()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);
        entities.Slot(2).Frame = 0x55;

        spawner.Spawn([Enemy(4, 4, 0)]);

        Assert.Equal(0x00, entities.Slot(2).Frame);
    }

    // $3A0F. Row bits 0-2 go to bits 1-3, and the move-left bit is rotated round into bit 0.
    [Fact]
    public void BuildsTheHeadingFromTheRowBitsAndTheMoveLeftBit()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(4, 4, 0, byte1Low: 5), Enemy(4, 4, 0, moveLeft: true, byte1Low: 5)]);

        Assert.Equal(0x0A, entities.Slot(2).Heading);
        Assert.Equal(0x0B, entities.Slot(3).Heading);
    }

    [Fact]
    public void DoublesTheDelay()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(4, 4, 0, delay: 63)]);

        Assert.Equal(0x7E, entities.Slot(2).HoldTimer);
    }

    // $3A0B adds with the carry $3A03 left. Column 30 is $F0, plus $14 is $104, and the row takes
    // the carry. No level has a column this far across.
    [Fact]
    public void CarriesFromTheColumnIntoTheRow()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(30, 2, 0)]);

        Assert.Equal(0x04, entities.Slot(2).X);
        Assert.Equal(0x26, entities.Slot(2).Y);
    }

    // $3A40. Only the low five bits of the draw are added to the table.
    [Fact]
    public void AddsFiveBitsOfTheDrawToTheTurnTimer()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0xFF);

        spawner.Spawn([Enemy(4, 4, 0)]);

        Assert.Equal(0x3D, entities.Slot(2).TurnTimer);
        Assert.Equal(0x3D, entities.Slot(2).TurnInterval);
    }

    // $1E2E clears State, X and Y of slots 2 to 7 and leaves the players alone.
    [Fact]
    public void ClearsTheEnemySlotsItDoesNotFill()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);
        for (int slot = 0; slot < EntityTable.Capacity; slot++)
        {
            entities.Slot(slot).State = 0x33;
            entities.Slot(slot).X = 0x33;
            entities.Slot(slot).Y = 0x33;
        }

        spawner.Spawn([Enemy(4, 4, 0)]);

        Assert.Equal([0x33, 0x33, 0x02, 0, 0, 0, 0, 0], entities.State.ToArray());
        Assert.Equal(0x33, entities.Slot(1).X);
        Assert.Equal(0x00, entities.Slot(7).X);
        Assert.Equal(0x00, entities.Slot(7).Y);
    }

    // $39D3, $39D6 and $3A67. The players' hold timers start at zero, and the count is one for each
    // enemy.
    [Fact]
    public void CountsTheEnemiesAndClearsThePlayersHoldTimers()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);
        entities.Slot(0).HoldTimer = 0x33;
        entities.Slot(1).HoldTimer = 0x33;
        entities.EnemyCount = 0x33;

        spawner.Spawn([Enemy(4, 4, 0), Enemy(5, 4, 0), Enemy(6, 4, 0)]);

        Assert.Equal(0x00, entities.Slot(0).HoldTimer);
        Assert.Equal(0x00, entities.Slot(1).HoldTimer);
        Assert.Equal(3, entities.EnemyCount);
    }

    private static (EntityTable Entities, EnemySpawner Spawner) Spawner(int timer)
    {
        EntityTable entities = new();
        return (entities, new(entities, new(new FakeRandomSource { RandomValue = timer })));
    }

    private static EnemySpawn Enemy(
        int x,
        int y,
        int type,
        int delay = 0,
        bool faceLeft = false,
        bool moveLeft = false,
        int byte1Low = 0)
        => new()
        {
            X = x,
            Y = y,
            Type = type,
            Delay = delay,
            FaceLeft = faceLeft,
            MoveLeft = moveLeft,
            Byte1Low = byte1Low,
        };
}

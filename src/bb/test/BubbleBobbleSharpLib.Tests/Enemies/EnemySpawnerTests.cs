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

        Assert.Equal(0x02, entities.State[2]);
        Assert.Equal(0x8C, entities.X[2]);
        Assert.Equal(0x15, entities.Y[2]);
        Assert.Equal(0x01, entities.Heading[2]);
        Assert.Equal(0x04, entities.Frame[2]);
        Assert.Equal(0x14, entities.HoldTimer[2]);
        Assert.Equal(0x04, entities.FrameCount[2]);
        Assert.Equal(0x03, entities.FrameMask[2]);
        Assert.Equal(0x73, entities.SpriteBase[2]);
        Assert.Equal(0x0C, entities.Colour[2]);
        Assert.Equal(0x1E, entities.TurnTimer[2]);
        Assert.Equal(0x1E, entities.TurnInterval[2]);
    }

    // Class 3 is state 5, the one DiagonalMover answers.
    [Fact]
    public void GivesClassThreeStateFive()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(4, 4, 3)]);

        Assert.Equal(0x05, entities.State[2]);
    }

    // State 9 reads each table's index 9, which is the next table's second byte. The frame count is
    // $AB81's second byte, $00, so the mask is $FF.
    [Fact]
    public void ReadsPastEachTableForClassSeven()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(4, 4, 7, faceLeft: true)]);

        Assert.Equal(0x09, entities.State[2]);
        Assert.Equal(0x0F, entities.Colour[2]);
        Assert.Equal(0xBB, entities.SpriteBase[2]);
        Assert.Equal(0x14, entities.TurnTimer[2]);
        Assert.Equal(0x00, entities.FrameCount[2]);
        Assert.Equal(0xFF, entities.FrameMask[2]);
        Assert.Equal(0x00, entities.Frame[2]);
    }

    [Fact]
    public void ReadsPastEachTableForClassSix()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(4, 4, 6, faceLeft: true)]);

        Assert.Equal(0x08, entities.State[2]);
        Assert.Equal(0x03, entities.Colour[2]);
        Assert.Equal(0xB3, entities.SpriteBase[2]);
        Assert.Equal(0x0A, entities.TurnTimer[2]);
        Assert.Equal(0x02, entities.FrameCount[2]);
        Assert.Equal(0x01, entities.FrameMask[2]);
        Assert.Equal(0x02, entities.Frame[2]);
    }

    // $3A21's `beq` stores the zero the `and #$40` left.
    [Fact]
    public void StartsAThingFacingRightOnFrameZero()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);
        entities.Frame[2] = 0x55;

        spawner.Spawn([Enemy(4, 4, 0)]);

        Assert.Equal(0x00, entities.Frame[2]);
    }

    // $3A0F. Row bits 0-2 go to bits 1-3, and the move-left bit is rotated round into bit 0.
    [Fact]
    public void BuildsTheHeadingFromTheRowBitsAndTheMoveLeftBit()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(4, 4, 0, byte1Low: 5), Enemy(4, 4, 0, moveLeft: true, byte1Low: 5)]);

        Assert.Equal(0x0A, entities.Heading[2]);
        Assert.Equal(0x0B, entities.Heading[3]);
    }

    [Fact]
    public void DoublesTheDelay()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(4, 4, 0, delay: 63)]);

        Assert.Equal(0x7E, entities.HoldTimer[2]);
    }

    // $3A0B adds with the carry $3A03 left. Column 30 is $F0, plus $14 is $104, and the row takes
    // the carry. No level has a column this far across.
    [Fact]
    public void CarriesFromTheColumnIntoTheRow()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);

        spawner.Spawn([Enemy(30, 2, 0)]);

        Assert.Equal(0x04, entities.X[2]);
        Assert.Equal(0x26, entities.Y[2]);
    }

    // $3A40. Only the low five bits of the draw are added to the table.
    [Fact]
    public void AddsFiveBitsOfTheDrawToTheTurnTimer()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0xFF);

        spawner.Spawn([Enemy(4, 4, 0)]);

        Assert.Equal(0x3D, entities.TurnTimer[2]);
        Assert.Equal(0x3D, entities.TurnInterval[2]);
    }

    // $1E2E clears State, X and Y of slots 2 to 7 and leaves the players alone.
    [Fact]
    public void ClearsTheEnemySlotsItDoesNotFill()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);
        for (int slot = 0; slot < EntityTable.Capacity; slot++)
        {
            entities.State[slot] = 0x33;
            entities.X[slot] = 0x33;
            entities.Y[slot] = 0x33;
        }

        spawner.Spawn([Enemy(4, 4, 0)]);

        Assert.Equal([0x33, 0x33, 0x02, 0, 0, 0, 0, 0], entities.State.ToArray());
        Assert.Equal(0x33, entities.X[1]);
        Assert.Equal(0x00, entities.X[7]);
        Assert.Equal(0x00, entities.Y[7]);
    }

    // $39D3, $39D6 and $3A67. The players' hold timers start at zero, and the count is one for each
    // enemy.
    [Fact]
    public void CountsTheEnemiesAndClearsThePlayersHoldTimers()
    {
        (EntityTable entities, EnemySpawner spawner) = Spawner(0x00);
        entities.HoldTimer[0] = 0x33;
        entities.HoldTimer[1] = 0x33;
        entities.EnemyCount = 0x33;

        spawner.Spawn([Enemy(4, 4, 0), Enemy(5, 4, 0), Enemy(6, 4, 0)]);

        Assert.Equal(0x00, entities.HoldTimer[0]);
        Assert.Equal(0x00, entities.HoldTimer[1]);
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

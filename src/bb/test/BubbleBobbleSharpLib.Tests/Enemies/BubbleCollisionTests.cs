// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// $105B and $1090: meeting an enemy, and taking it out of the eight slots.
//
// Worked by hand off the reference. No enemy has been spawned in the port yet, so there is nothing
// to capture from VICE and nothing here is proved against the game.
public sealed class BubbleCollisionTests
{
    private const int Slot = 17;

    private const byte ThingX = 0x44;
    private const byte ThingY = 0x60;

    // The last of the six, which is slot 7 of the eight, and the one $105B reaches first.
    private const int LastEnemy = 7;

    // A state below $0B that is not $0A, so the walk tests it and the capture takes the short path.
    private const byte PlainState = 0x03;

    // $1099. At or above this the capture takes the long path and resets the three counters.
    private const byte ResetState = 0x16;

    // $105B walks the six enemies from five down to zero, so slot 7 is tried before slot 2 and the
    // first one within range ends the walk.
    [Fact]
    public void TakesTheLastEnemyFirst()
    {
        (_, EntityTable entities, BubbleCollision collision) = Thing();
        Enemy(entities, LastEnemy, PlainState);
        Enemy(entities, 2, PlainState);

        Assert.True(collision.Check(Slot));

        Assert.Equal(0, entities.Slot(LastEnemy).State);
        Assert.Equal(PlainState, entities.Slot(2).State);
    }

    // $105B starts at five, which is slot 7, and stops at zero, which is slot 2. The two players sit
    // in slots 0 and 1 and are never looked at - a walk that ran to the bottom of the eight would
    // swallow a player standing where a bubble is.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void NeverLooksAtAPlayer(int player)
    {
        (ObjectTable objects, EntityTable entities, BubbleCollision collision) = Thing();
        Enemy(entities, player, PlainState);

        Assert.False(collision.Check(Slot));

        Assert.Equal(PlainState, entities.Slot(player).State);
        Assert.Equal(ObjectTable.FreeType, objects.Slot(Slot).Type);
    }

    // $105D and $1060. An empty slot is passed over, and so is one in the band $0B to $15. Anything
    // else is tested, including $0A just below the band and anything at or above $16.
    [Theory]
    [InlineData(0x00, false)]
    [InlineData(0x0B, false)]
    [InlineData(0x15, false)]
    [InlineData(0x0A, true)]
    [InlineData(0x16, true)]
    [InlineData(PlainState, true)]
    public void TestsOnlyTheStatesThatAreThereToBeHit(byte state, bool hit)
    {
        (_, EntityTable entities, BubbleCollision collision) = Thing();
        Enemy(entities, LastEnemy, state);

        Assert.Equal(hit, collision.Check(Slot));
    }

    // $1077 and $1088: `cmp #$10` then a branch, so fifteen is in and sixteen is out. Both axes, and
    // neither is nudged - unlike the catch at $0D02, which shifts its box two pixels in X.
    [Theory]
    [InlineData(0x53, ThingY, true)]
    [InlineData(0x54, ThingY, false)]
    [InlineData(ThingX, 0x6F, true)]
    [InlineData(ThingX, 0x70, false)]
    public void TakesFifteenPixelsAndNotSixteen(byte x, byte y, bool hit)
    {
        (_, EntityTable entities, BubbleCollision collision) = Thing();
        Enemy(entities, LastEnemy, PlainState);
        entities.Slot(LastEnemy).X = x;
        entities.Slot(LastEnemy).Y = y;

        Assert.Equal(hit, collision.Check(Slot));
    }

    // $1091's `asl` then `adc #$18`. Which of the six it was survives in the type byte, doubled and
    // offset, so the six give $18 to $22 in twos.
    [Theory]
    [InlineData(2, 0x18)]
    [InlineData(3, 0x1A)]
    [InlineData(7, 0x22)]
    public void KeepsWhichEnemyItWasInTheTypeByte(int enemy, byte type)
    {
        (ObjectTable objects, EntityTable entities, BubbleCollision collision) = Thing();
        Enemy(entities, enemy, PlainState);

        _ = collision.Check(Slot);

        Assert.Equal(type, objects.Slot(Slot).Type);
    }

    // $1095 and $10B1. On the short path nothing loads the accumulator between them, so what reaches
    // $AA30 is the enemy's own state byte and the three counters are left alone.
    [Fact]
    public void CarriesTheStateByteOnTheShortPath()
    {
        (ObjectTable objects, EntityTable entities, BubbleCollision collision) = Thing();
        Enemy(entities, LastEnemy, PlainState);
        entities.Slot(LastEnemy).RiseCounter = 0x05;

        _ = collision.Check(Slot);

        Assert.Equal(PlainState, objects.Slot(Slot).Variant);
        Assert.Equal(0x05, entities.Slot(LastEnemy).RiseCounter);
    }

    // $10A1. On the long path the rise counter is read out first and the three counters go to $FF,
    // so it is the *old* rise counter that reaches $AA30 and not the state byte.
    //
    // This is the trap. Both paths end at the same `sta D_AA30,x`, and which byte is in the
    // accumulator was decided fifteen instructions earlier by a `cmp` that does not touch it.
    [Theory]
    [InlineData(0x0A)]
    [InlineData(ResetState)]
    public void CarriesTheOldRiseCounterOnTheLongPath(byte state)
    {
        (ObjectTable objects, EntityTable entities, BubbleCollision collision) = Thing();
        Enemy(entities, LastEnemy, state);
        entities.Slot(LastEnemy).RiseCounter = 0x05;

        _ = collision.Check(Slot);

        Assert.Equal(0x05, objects.Slot(Slot).Variant);
        Assert.Equal(0xFF, entities.Slot(LastEnemy).RiseCounter);
        Assert.Equal(0xFF, entities.Slot(LastEnemy).FallCounter);
        Assert.Equal(0xFF, entities.Slot(LastEnemy).GroundState);
    }

    // $10B4. The enemy is emptied out of the eight and the slot that caught it loses its AI counter,
    // so the dispatcher will not step it again.
    [Fact]
    public void EmptiesTheEnemyAndStopsTheSlot()
    {
        (ObjectTable objects, EntityTable entities, BubbleCollision collision) = Thing();
        Enemy(entities, LastEnemy, PlainState);
        entities.Slot(LastEnemy).HoldTimer = 0x04;
        objects.Slot(Slot).State = 0x06;

        _ = collision.Check(Slot);

        Assert.Equal(0, entities.Slot(LastEnemy).State);
        Assert.Equal(0, entities.Slot(LastEnemy).HoldTimer);
        Assert.Equal(0xFF, entities.Slot(LastEnemy).Mode);
        Assert.Equal(0, objects.Slot(Slot).State);
        Assert.Equal(0xA0, objects.Slot(Slot).Flags);
    }

    // $10BF. The frame is indexed by whichever byte reached $AA30, so the two paths above pick
    // different entries of the same table.
    [Fact]
    public void ReadsTheScoreWithTheByteThatReachedTheVariant()
    {
        (ObjectTable objects, EntityTable entities, BubbleCollision collision) = Thing();
        Enemy(entities, LastEnemy, ResetState);
        entities.Slot(LastEnemy).RiseCounter = 0x05;

        _ = collision.Check(Slot);

        Assert.Equal(0x07, objects.Slot(Slot).EnemyType);
    }

    // $AB81 is eight bytes, and the game reads on into $AB89 for enemies in states 8 and 9, which
    // the game loop reaches. Those bytes are the image's own.
    [Theory]
    [InlineData(0x08, 0x07)]
    [InlineData(0x09, 0x05)]
    public void ReadsOnIntoTheNextTableForTheLastTwoClasses(byte state, byte frame)
    {
        (ObjectTable objects, EntityTable entities, BubbleCollision collision) = Thing();
        Enemy(entities, LastEnemy, state);

        collision.Check(Slot);

        Assert.Equal(frame, objects.Slot(Slot).EnemyType);
    }

    // Past $AB89 the next table is text. An enemy still carrying the $FF its rise counter starts a
    // level at would walk off the end, and nothing has shown what the game does there, so this fails
    // loudly rather than invent the bytes.
    [Fact]
    public void ThrowsRatherThanReadPastTheScoreTable()
    {
        (_, EntityTable entities, BubbleCollision collision) = Thing();
        Enemy(entities, LastEnemy, ResetState);
        entities.Slot(LastEnemy).RiseCounter = 0xFF;

        _ = Assert.Throws<IndexOutOfRangeException>(() => collision.Check(Slot));
    }

    // One enemy sitting exactly on the thing, so that only the state under test decides the outcome.
    private static void Enemy(EntityTable entities, int slot, byte state)
    {
        entities.Slot(slot).State = state;
        entities.Slot(slot).X = ThingX;
        entities.Slot(slot).Y = ThingY;
    }

    private static (ObjectTable Objects, EntityTable Entities, BubbleCollision Collision) Thing()
    {
        ObjectTable objects = new();
        objects.Slot(Slot).X = ThingX;
        objects.Slot(Slot).Y = ThingY;

        EntityTable entities = new();

        return (objects, entities, new BubbleCollision(objects, entities));
    }
}

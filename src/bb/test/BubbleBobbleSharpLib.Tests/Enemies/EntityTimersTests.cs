// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// $13BE: the clock each slot runs on, the wobble at the end of it, and the pop after that.
//
// Two of the four phases the VICE capture recorded are here - the wobble at frames 195 to 215 and
// the pop that follows - so these are the first cases in Phase 6 that a capture speaks to at all.
// It is still not a step-for-step match: the capture's frame numbers and the game loop's passes are
// not the same clock, which is the open question in docs/bb-port-plan.md.
public sealed class EntityTimersTests
{
    private const int Slot = 17;

    private const byte Bubble = 0x04;
    private const byte Wobbling = 0x48;
    private const byte Popping = 0x3A;
    private const byte Freed = 0x38;
    private const byte Released = 0x09;

    // $13C0's `dec`. One off every slot, every pass.
    [Fact]
    public void TakesOneOffTheClock()
    {
        (ObjectTable objects, _, EntityTimers timers) = Table();
        objects.Flags[Slot] = 0x7D;

        timers.Update(Released);

        Assert.Equal(0x7C, objects.Flags[Slot]);
    }

    // $13C6. A bubble whose clock runs out keeps what it was and starts popping. This is what ends
    // a bubble - nothing in enemy-ai.s expires anything.
    [Fact]
    public void PopsABubbleWhoseClockRunsOut()
    {
        (ObjectTable objects, _, EntityTimers timers) = Table();
        objects.Type[Slot] = Bubble;
        objects.Flags[Slot] = 0x01;

        timers.Update(Released);

        Assert.Equal(Popping, objects.Type[Slot]);
        Assert.Equal(Bubble, objects.EnemyType[Slot]);
    }

    // $1428. The last sixteen counts, on the even ones only, and $04 eor $4C does both halves of
    // the toggle. This is the wobble the capture read.
    [Theory]
    [InlineData(Bubble, Wobbling)]
    [InlineData(Wobbling, Bubble)]
    public void WobblesNearTheEndOfTheClock(byte type, byte expected)
    {
        (ObjectTable objects, _, EntityTimers timers) = Table();
        objects.Type[Slot] = type;
        objects.Flags[Slot] = 0x0B;

        timers.Update(Released);

        Assert.Equal(expected, objects.Type[Slot]);
    }

    // $142E. A caught enemy flickers instead: its own slot's bit in $D015 flips, through $AB55, and
    // back again the next even count. $18 is enemy 0 in slot 2; $23 is enemy 5 in slot 7.
    [Theory]
    [InlineData(0x18, 0xFB)]
    [InlineData(0x1C, 0xEF)]
    [InlineData(0x23, 0x7F)]
    public void FlickersACaughtEnemy(byte type, byte enabled)
    {
        (ObjectTable objects, EntityTable entities, EntityTimers timers) = Table();
        objects.Type[Slot] = type;
        objects.Flags[Slot] = 0x0D;
        entities.SpriteEnable = 0xFF;

        timers.Update(Released);
        Assert.Equal(enabled, entities.SpriteEnable);

        timers.Update(Released);
        Assert.Equal(enabled, entities.SpriteEnable);

        timers.Update(Released);
        Assert.Equal(0xFF, entities.SpriteEnable);
        Assert.Equal(type, objects.Type[Slot]);
    }

    // $1415 and $1419. An odd count does nothing, and neither does any count from $11 up - so the
    // wobble is half speed and only starts near the end.
    [Theory]
    [InlineData(0x0C)]
    [InlineData(0x20)]
    public void DoesNotWobbleOnAnOddCountOrAnEarlyOne(byte clock)
    {
        (ObjectTable objects, _, EntityTimers timers) = Table();
        objects.Type[Slot] = Bubble;
        objects.Flags[Slot] = clock;

        timers.Update(Released);

        Assert.Equal(Bubble, objects.Type[Slot]);
    }

    // $13DA. A bubble with an enemy in it lets the enemy back into the eight slots, and $13DC undoes
    // exactly what BubbleCollision's $1091 encoded.
    [Theory]
    [InlineData(0x18, 2)]
    [InlineData(0x1A, 3)]
    [InlineData(0x22, 7)]
    public void PutsACapturedEnemyBackInItsOwnSlot(byte type, int entity)
    {
        (ObjectTable objects, EntityTable entities, EntityTimers timers) = Table();
        objects.Type[Slot] = type;
        objects.Flags[Slot] = 0x01;
        objects.Variant[Slot] = 0x55;

        timers.Update(Released);

        Assert.Equal(Released, entities.State[entity]);
        Assert.Equal(0x55, entities.RiseCounter[entity]);
        Assert.Equal(0xFF, entities.FlashTimer[entity]);
        Assert.Equal(0, entities.Frame[entity]);
        Assert.Equal(Freed, objects.Type[Slot]);
    }

    // $13E8 and $13EE. The row gets its bottom bit set and the column gets its bottom bit cleared -
    // the reference's own asymmetry, and a pair tidied into a matching pair puts every released
    // enemy in the wrong place.
    [Fact]
    public void SetsTheRowBitAndClearsTheColumnBit()
    {
        (ObjectTable objects, EntityTable entities, EntityTimers timers) = Table();
        objects.Type[Slot] = 0x18;
        objects.Flags[Slot] = 0x01;
        entities.X[2] = 0x45;
        entities.Y[2] = 0x60;

        timers.Update(Released);

        Assert.Equal(0x44, entities.X[2]);
        Assert.Equal(0x61, entities.Y[2]);
    }

    // $140B. Above $24 only one value means anything, and it frees the slot outright.
    [Theory]
    [InlineData(0x42, ObjectTable.FreeType)]
    [InlineData(0x40, 0x40)]
    public void FreesOnlyTheOneHighType(byte type, byte expected)
    {
        (ObjectTable objects, _, EntityTimers timers) = Table();
        objects.Type[Slot] = type;
        objects.Flags[Slot] = 0x01;

        timers.Update(Released);

        Assert.Equal(expected, objects.Type[Slot]);
    }

    // $144D counts every slot at or above $34 - and a free slot is $FF, which is above $34. So the
    // count includes the empty table, and with any slot free at all it reaches two immediately and
    // the pressure pass does nothing.
    //
    // That makes $1449 very nearly dead code in ordinary play: eighteen slots are never all full.
    // It reads like a mechanism that stops a player parking the level full of bubbles, and on these
    // bytes it cannot fire until they have done exactly that.
    [Fact]
    public void CountsAFreeSlotAsAHeldEnemy()
    {
        (ObjectTable objects, _, EntityTimers timers) = Table();
        objects.Type[16] = Bubble;

        timers.Update(Released);

        Assert.Equal(Bubble, objects.Type[16]);
    }

    // $145C is a `jsr` to the very next instruction, so $145F runs twice - once through the call and
    // once by falling into it. Two bubbles a pass, not one.
    //
    // Reaching it at all needs every one of the eighteen slots below $34, which is what the fill
    // here does.
    [Fact]
    public void PopsTwoBubblesAPassOnceNothingIsFree()
    {
        (ObjectTable objects, _, EntityTimers timers) = Table();
        objects.Type.Fill(Bubble);

        timers.Update(Released);

        Assert.Equal(Popping, objects.Type[17]);
        Assert.Equal(Popping, objects.Type[16]);
        Assert.Equal(Bubble, objects.Type[15]);
    }

    // A cleared table, so every slot but the one under test is free and stays that way.
    private static (ObjectTable Objects, EntityTable Entities, EntityTimers Timers) Table()
    {
        ObjectTable objects = new();
        EntityTable entities = new();

        return (objects, entities, new EntityTimers(objects, entities));
    }
}

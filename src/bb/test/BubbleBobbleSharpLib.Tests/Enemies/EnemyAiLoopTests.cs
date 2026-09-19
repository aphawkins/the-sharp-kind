// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// $0CF2: the walk over the eighteen slots, and the two decisions it makes on its own.
//
// What the loop reaches is not tested here, because it is not translated yet. These prove the walk,
// the special-type gate, and the catch - and in particular the two places a careful reading of the
// reference gives the wrong answer: the catch box is nudged two pixels in one axis only, and a
// caught slot's own new type puts it beyond the gate.
public sealed class EnemyAiLoopTests
{
    private const int Slot = 17;
    private const byte Bubble = 0x16;
    private const byte Caught = 0x34;
    private const byte Baron = 0x44;

    // A player standing well away from the origin, so that a difference that underflows is a
    // different number from one that does not.
    private const byte PlayerX = 0x40;
    private const byte PlayerY = 0x40;

    // A level with nothing solid in it. None of these tests reaches the dispatcher, so what the map
    // holds does not matter - it only has to exist.
    private static readonly SolidMap s_open = SolidMap.Build(new Level
    {
        Number = 1,
        Bitmap = [.. Enumerable.Repeat(new string('.', 32), 23)],
    });

    [Fact]
    public void LeavesAnEmptyTableAlone()
    {
        (ObjectTable objects, _, EnemyAiLoop loop) = Table();

        loop.Update(s_open);

        foreach (byte type in objects.Type)
        {
            Assert.Equal(ObjectTable.FreeType, type);
        }
    }

    // $0CF4's gate. A free slot is $FF, which is above $24, so every slot in a cleared table goes
    // to $0D4E and none of them runs any AI at all.
    [Theory]
    [InlineData(0x24)]
    [InlineData(0x33)]
    [InlineData(Baron)]
    [InlineData(0x4C)]
    [InlineData(ObjectTable.FreeType)]
    public void LeavesASpecialTypeAlone(byte type)
    {
        (ObjectTable objects, _, EnemyAiLoop loop) = Table();
        Standing(objects);
        objects.Type[Slot] = type;

        loop.Update(s_open);

        Assert.Equal(type, objects.Type[Slot]);
    }

    // $0CFC. A slot with an AI state goes to the movement dispatcher instead of being tested for a
    // catch, and it goes there even though a player is sitting right on top of it.
    //
    // The two assertions are both the dispatcher's: it spends one of the counter and it moves the
    // thing eight pixels. Neither the catch nor the loop touches either byte.
    [Fact]
    public void HandsASlotWithAnAiStateToTheDispatcher()
    {
        (ObjectTable objects, EntityTable entities, EnemyAiLoop loop) = Table();
        Standing(objects);
        objects.Row[Slot] = 0x0A;
        objects.Column[Slot] = 0x05;
        objects.State[Slot] = 0x06;
        entities.State[0] = 0x01;

        loop.Update(s_open);

        Assert.Equal(0x05, objects.State[Slot]);
        Assert.Equal(PlayerX + 8, objects.X[Slot]);
        Assert.NotEqual(Caught, objects.Type[Slot]);
    }

    // $0D33. Three stores, and this asserts all three.
    [Fact]
    public void CatchesAPlayerWithinRange()
    {
        (ObjectTable objects, EntityTable entities, EnemyAiLoop loop) = Table();
        Standing(objects);
        entities.State[0] = 0x01;

        loop.Update(s_open);

        Assert.Equal(Caught, objects.Type[Slot]);
        Assert.Equal(Bubble, objects.EnemyType[Slot]);
        Assert.Equal(0, objects.Direction[Slot]);
    }

    // $0D02's `ldy #$01`. Player two is tested first, so with both in range player two wins. A loop
    // written upwards gives player one and every test above still passes.
    [Fact]
    public void ChecksPlayerTwoFirst()
    {
        (ObjectTable objects, EntityTable entities, EnemyAiLoop loop) = Table();
        Standing(objects);
        entities.State[0] = 0x01;
        entities.State[1] = 0x01;

        loop.Update(s_open);

        Assert.Equal(1, objects.Direction[Slot]);
    }

    // $0D06, $0D0A and $0D0E. Empty, dying and dead are all passed over.
    [Theory]
    [InlineData(0x00)]
    [InlineData(0x0E)]
    [InlineData(0x0F)]
    public void DoesNotCatchForAPlayerInThatState(byte state)
    {
        (ObjectTable objects, EntityTable entities, EnemyAiLoop loop) = Table();
        Standing(objects);
        entities.State[0] = state;

        loop.Update(s_open);

        Assert.Equal(Bubble, objects.Type[Slot]);
    }

    // $0D21 and $0D32: `cmp #$10` then `bcs`, so sixteen is out and fifteen is in. The bound is the
    // same in both axes even though what is compared is not.
    [Theory]
    [InlineData(0x51, PlayerY, true)]
    [InlineData(0x52, PlayerY, false)]
    [InlineData(PlayerX, 0x4F, true)]
    [InlineData(PlayerX, 0x50, false)]
    public void TakesFifteenPixelsAndNotSixteen(byte x, byte y, bool caught)
    {
        (ObjectTable objects, EntityTable entities, EnemyAiLoop loop) = Table();
        Standing(objects);
        objects.X[Slot] = x;
        objects.Y[Slot] = y;
        entities.State[0] = 0x01;

        loop.Update(s_open);

        Assert.Equal(caught ? Caught : Bubble, objects.Type[Slot]);
    }

    // $0D14's `clc / adc #$02`. The player's X is nudged two pixels right before the subtraction and
    // their Y is not, so the catch box is not centred on the player. A slot two pixels left of the
    // box's left edge is out of range going left, while its mirror going right is still in.
    //
    // This is the asymmetry a mirrored reading smooths away. Both slots below are the same distance
    // from the player's own X; only one of them is caught.
    [Fact]
    public void NudgesThePlayerXAndNotThePlayerY()
    {
        (ObjectTable left, EntityTable leftEntities, EnemyAiLoop leftLoop) = Table();
        Standing(left);
        left.X[Slot] = 0x32;
        leftEntities.State[0] = 0x01;

        (ObjectTable right, EntityTable rightEntities, EnemyAiLoop rightLoop) = Table();
        Standing(right);
        right.X[Slot] = 0x4E;
        rightEntities.State[0] = 0x01;

        leftLoop.Update(s_open);
        rightLoop.Update(s_open);

        Assert.Equal(Bubble, left.Type[Slot]);
        Assert.Equal(Caught, right.Type[Slot]);
    }

    // The catch writes $34, which is itself above $0CF4's gate of $24. So the next frame routes the
    // slot to $0D4E and the stored type underneath it survives. A loop that caught twice would
    // overwrite the variant with $34 and lose what the thing was.
    [Fact]
    public void DoesNotCatchTheSameSlotTwice()
    {
        (ObjectTable objects, EntityTable entities, EnemyAiLoop loop) = Table();
        Standing(objects);
        entities.State[0] = 0x01;

        loop.Update(s_open);
        loop.Update(s_open);

        Assert.Equal(Caught, objects.Type[Slot]);
        Assert.Equal(Bubble, objects.EnemyType[Slot]);
    }

    // Every slot is walked, not just the first: the loop runs from seventeen down to zero.
    [Fact]
    public void WalksEverySlot()
    {
        (ObjectTable objects, EntityTable entities, EnemyAiLoop loop) = Table();
        entities.State[0] = 0x01;
        entities.X[0] = PlayerX;
        entities.Y[0] = PlayerY;

        for (int slot = 0; slot < ObjectTable.Capacity; slot++)
        {
            objects.Type[slot] = Bubble;
            objects.X[slot] = PlayerX;
            objects.Y[slot] = PlayerY;
        }

        loop.Update(s_open);

        foreach (byte type in objects.Type)
        {
            Assert.Equal(Caught, type);
        }
    }

    // A bubble in the top slot, sitting exactly on the player, with neither player active yet.
    private static void Standing(ObjectTable objects)
    {
        objects.Type[Slot] = Bubble;
        objects.X[Slot] = PlayerX;
        objects.Y[Slot] = PlayerY;
    }

    private static EnemyDispatcher Dispatcher(ObjectTable objects, EntityTable entities)
        => new(objects, entities, new BubbleCollision(objects, entities));

    private static (ObjectTable Objects, EntityTable Entities, EnemyAiLoop Loop) Table()
    {
        ObjectTable objects = new();
        EntityTable entities = new();
        entities.X[0] = PlayerX;
        entities.Y[0] = PlayerY;
        entities.X[1] = PlayerX;
        entities.Y[1] = PlayerY;

        return (objects, entities, new EnemyAiLoop(objects, entities, Dispatcher(objects, entities)));
    }
}

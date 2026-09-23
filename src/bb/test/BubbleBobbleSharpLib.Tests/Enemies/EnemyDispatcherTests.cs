// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// $0F48, $0F61, $0F91, $0F98 and $100B: the eight-pixel half of entity movement.
//
// Worked by hand off the reference. The bubble capture in docs/bb-port-plan.md shows a bubble
// shooting right eight pixels a move, which is this, but the capture was read a frame at a time and
// this routine can run twice in a frame - so it cannot be asserted step for step the way
// EntityMoverTests asserts the rise. What is here instead is each arm and each trap on its own.
public sealed class EnemyDispatcherTests
{
    private const int Slot = 17;

    // A row in the middle of the level, so that every probe lands on the map rather than off it.
    // The entity row is four more than the map row - see RowBias.
    private const byte EntityRow = 0x0A;
    private const int MapRow = EntityRow - 4;

    private const byte StartColumn = 0x05;
    private const byte StartX = 0x44;
    private const byte StartY = 0x60;

    // $ACB6. Held here so the expectations read as the table rather than as bare numbers.
    private const byte TypeAtSix = 0x00;
    private const byte TypeAtFive = 0x02;
    private const byte TypeAtThree = 0x02;
    private const byte TypeAtOne = 0x04;

    private const byte MovedType = 0x04;
    private const byte CaughtType = 0x34;

    // $0F91's `lda D_ACB6,y` with a counter of six gives the first byte of the table, $00.
    //
    // This is the test that catches the trap. $0F61 decrements the counter and then masks the
    // *accumulator*, which still holds the value from before the decrement. A translation that
    // indexes with the decremented counter reads $02 here instead, and every animation in the game
    // is one frame ahead of the machine.
    [Fact]
    public void IndexesWithTheCounterFromBeforeTheDecrement()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x06;

        dispatcher.Step(Slot, Open());

        Assert.Equal(TypeAtSix, objects.Type[Slot]);
    }

    // $0F62's `dec`. One of the counter is spent per step, and $1001 clears its top bit on a step
    // that was not blocked.
    [Fact]
    public void SpendsOneOfTheCounter()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x06;

        dispatcher.Step(Slot, Open());

        Assert.Equal(0x05, objects.State[Slot]);
    }

    // $0FD1. Eight pixels and one column to the right, because $0193 is positive.
    [Fact]
    public void StepsRightWhenTheDirectionByteIsPositive()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x06;

        dispatcher.Step(Slot, Open());

        Assert.Equal(StartX + 8, objects.X[Slot]);
        Assert.Equal(StartColumn + 1, objects.Column[Slot]);
    }

    // $0FA6. The same, the other way, when $0193 has its top bit set.
    [Fact]
    public void StepsLeftWhenTheDirectionByteIsNegative()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x06;
        objects.Direction[Slot] = 0x80;

        dispatcher.Step(Slot, Open());

        Assert.Equal(StartX - 8, objects.X[Slot]);
        Assert.Equal(StartColumn - 1, objects.Column[Slot]);
    }

    // $0F53. A slot whose enemy type byte is negative steps twice in one call, so it covers two
    // columns rather than one - and the second step indexes with the counter the first left behind.
    [Fact]
    public void StepsTwiceForANegativeEnemyType()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x06;
        objects.EnemyType[Slot] = 0x80;

        dispatcher.Step(Slot, Open());

        Assert.Equal(StartColumn + 2, objects.Column[Slot]);
        Assert.Equal(0x04, objects.State[Slot]);
        Assert.Equal(TypeAtFive, objects.Type[Slot]);
    }

    // $0F66. A slot whose $AA30 is negative halves its index before it reads the table, so a
    // counter of six reads the table's fourth byte and not its seventh.
    [Fact]
    public void HalvesTheIndexForANegativeVariant()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x06;
        objects.Variant[Slot] = 0x80;

        dispatcher.Step(Slot, Open());

        Assert.Equal(TypeAtThree, objects.Type[Slot]);
    }

    // $0F6E. A half that reaches zero is held at one instead, so the table's first byte is never
    // reached down this arm.
    [Fact]
    public void HoldsAHalvedIndexAtOne()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x01;
        objects.Variant[Slot] = 0x80;

        dispatcher.Step(Slot, Open());

        Assert.Equal(TypeAtOne, objects.Type[Slot]);
    }

    // $0F91 indexes seven bytes with a masked counter that can be up to $7F, and the 6502 would
    // read whatever follows the table. This fails loudly instead.
    [Fact]
    public void ThrowsRatherThanReadPastTheTable()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x10;
        SolidMap map = Open();

        _ = Assert.Throws<IndexOutOfRangeException>(() => dispatcher.Step(Slot, map));
    }

    // $0FD3. The right arm refuses to leave column $1C, and it is blocked before it moves at all.
    // The left arm has no matching bound, which is why this is asserted one way only.
    [Fact]
    public void WillNotStepPastTheLastColumn()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x06;
        objects.Column[Slot] = 0x1C;

        dispatcher.Step(Slot, Open());

        Assert.Equal(0x1C, objects.Column[Slot]);
        Assert.Equal(MovedType, objects.Type[Slot]);
        Assert.Equal(0, objects.State[Slot]);
    }

    // $100E. The blocked thing is put back on the eight pixel grid. The `adc #$13` runs with the
    // carry the subtraction left, so it adds twenty: $45 comes back as $44 and not as $43.
    [Fact]
    public void SnapsABlockedThingBackToItsCell()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x06;
        objects.Column[Slot] = 0x1C;
        objects.X[Slot] = 0x45;

        dispatcher.Step(Slot, Open());

        Assert.Equal(0x44, objects.X[Slot]);
    }

    // $0FF6's `ldy #$28`, the cell below the one just stepped into. A solid cell there puts the
    // thing back where it started.
    [Fact]
    public void IsBlockedByASolidCellBelowTheStep()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x06;

        dispatcher.Step(Slot, Solid(MapRow + 1, StartColumn + 1));

        Assert.Equal(StartColumn, objects.Column[Slot]);
        Assert.Equal(StartX, objects.X[Slot]);
        Assert.Equal(MovedType, objects.Type[Slot]);
    }

    // $0FB4's `beq L0FC7`. The first of the three probes is skipped when the vertical sub-position
    // is zero - when the thing sits exactly on a cell boundary. The same solid cell blocks it only
    // once it is part way into the cell.
    //
    // The byte that decides this is $A9D6, which the reference calls a direction and which
    // EntityMover proves is the sub-position.
    [Theory]
    [InlineData(0x00, StartColumn + 1)]
    [InlineData(0x02, StartColumn)]
    public void SkipsTheFirstProbeOnACellBoundary(byte subY, int column)
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x06;
        objects.SubY[Slot] = subY;

        dispatcher.Step(Slot, Solid(MapRow, StartColumn + 2));

        Assert.Equal(column, objects.Column[Slot]);
    }

    // $1016. A blocked thing whose counter is still negative after the decrement is caught, and the
    // byte it used to be is kept underneath. $81 is used because $80 would be positive once
    // decremented.
    [Fact]
    public void CatchesABlockedThingWithANegativeCounter()
    {
        (ObjectTable objects, _, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x81;
        objects.Column[Slot] = 0x1C;

        dispatcher.Step(Slot, Open());

        Assert.Equal(CaughtType, objects.Type[Slot]);
        Assert.Equal(MovedType, objects.EnemyType[Slot]);
        Assert.Equal(0, objects.State[Slot]);
    }

    // $101C. The proximity test is against player two and nobody else - $BB and $C3 are the second
    // byte of each array and there is no index register in sight. It decides the byte written to
    // $0193, and it does not decide whether the thing is caught: it is caught either way.
    [Theory]
    [InlineData(0x40, 1)]
    [InlineData(0x00, 0)]
    public void RecordsWhetherPlayerTwoWasNear(byte playerX, byte direction)
    {
        (ObjectTable objects, EntityTable entities, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x81;
        objects.Column[Slot] = 0x1C;
        entities.X[1] = playerX;
        entities.Y[1] = StartY;

        dispatcher.Step(Slot, Open());

        Assert.Equal(direction, objects.Direction[Slot]);
        Assert.Equal(CaughtType, objects.Type[Slot]);
    }

    // $101C again, this time proving player one has no say. Player one is put right on top of the
    // thing and player two far away, and the answer is still that nobody was near.
    [Fact]
    public void IgnoresPlayerOneEntirely()
    {
        (ObjectTable objects, EntityTable entities, EnemyDispatcher dispatcher) = Entity();
        objects.State[Slot] = 0x81;
        objects.Column[Slot] = 0x1C;
        entities.X[0] = 0x44;
        entities.Y[0] = StartY;
        entities.X[1] = 0x00;
        entities.Y[1] = 0x00;

        dispatcher.Step(Slot, Open());

        Assert.Equal(0, objects.Direction[Slot]);
    }

    private static SolidMap Open() => SolidMap.Build(Level(), []);

    private static SolidMap Solid(int row, int column) => SolidMap.Build(Level((row, column)), []);

    // SolidMap puts the level's own twenty-three rows below a solid ceiling, so a map row is one
    // more than the bitmap line it came from.
    private static Level Level(params (int Row, int Column)[] cells)
    {
        List<string> bitmap = [.. Enumerable.Repeat(new string('.', 32), 23)];

        foreach ((int row, int column) in cells)
        {
            bitmap[row - 1] = $"{bitmap[row - 1][..column]}#{bitmap[row - 1][(column + 1)..]}";
        }

        return new() { Number = 1, Bitmap = bitmap };
    }

    // One thing part way along an open row, facing right, with both players out of the way.
    private static (ObjectTable Objects, EntityTable Entities, EnemyDispatcher Dispatcher) Entity()
    {
        ObjectTable objects = new();
        objects.Type[Slot] = 0x00;
        objects.Row[Slot] = EntityRow;
        objects.Column[Slot] = StartColumn;
        objects.X[Slot] = StartX;
        objects.Y[Slot] = StartY;

        EntityTable entities = new();

        return (objects, entities, new EnemyDispatcher(objects, entities, new BubbleCollision(objects, entities)));
    }
}

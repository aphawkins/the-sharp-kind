// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// $1473, worked by hand off the reference. The Baron was seen in VICE, but no pass of his was
// captured, so these prove the translation against what is written.
public sealed class BaronTests
{
    // Player 1's Baron, in ObjectTable slot 0.
    private const int Slot = 0;

    // Column 10, row 5. His corner is $50 + $18 = $68 across and $28 + $15 = $3D down.
    private const byte Column = 0x0A;
    private const byte Row = 0x05;

    // Player 1 in the cell below and to the right: column 20, row 12, far out of reach.
    private const byte FarX = 0xB4;
    private const byte FarY = 0x75;

    [Fact]
    public void RoutesOnlyTheTwoBaronTypes()
    {
        Assert.True(Baron.Is(0x2E));
        Assert.True(Baron.Is(0x30));
        Assert.False(Baron.Is(0x44));
        Assert.False(Baron.Is(0x3A));
    }

    // $1475. His player dying, and he goes.
    [Fact]
    public void GoesWhenHisPlayerDies()
    {
        (ObjectTable objects, EntityTable entities, Baron baron) = Moving();
        entities.State[Slot] = 0x0E;

        baron.Step(Slot);

        Assert.Equal(0x3A, objects.Type[Slot]);
        Assert.Equal(0x3A, objects.EnemyType[Slot]);
    }

    // $152B. Within fifteen pixels of his corner in both axes, and the player dies and he goes.
    [Theory]
    [InlineData(0x68, 0x3D, true)]
    [InlineData(0x77, 0x4C, true)]
    [InlineData(0x59, 0x2E, true)]
    [InlineData(0x78, 0x3D, false)]
    [InlineData(0x68, 0x2D, false)]
    public void CatchesAPlayerWithinSixteenPixels(int x, int y, bool caught)
    {
        (ObjectTable objects, EntityTable entities, Baron baron) = Moving();
        entities.X[Slot] = (byte)x;
        entities.Y[Slot] = (byte)y;

        baron.Step(Slot);

        Assert.Equal(caught ? 0x0E : 0x01, entities.State[Slot]);
        Assert.Equal(caught ? 0x3A : 0x2E, objects.Type[Slot]);
    }

    // $1496's two compares. Empty, and $0E and above, are safe - except $18.
    [Theory]
    [InlineData(0x00, false)]
    [InlineData(0x0D, true)]
    [InlineData(0x0F, false)]
    [InlineData(0x18, true)]
    [InlineData(0x19, false)]
    public void CatchesOnlyLiveStatesAndEighteen(int state, bool caught)
    {
        (ObjectTable objects, EntityTable entities, Baron baron) = Moving();
        entities.State[1] = (byte)state;
        entities.X[1] = 0x68;
        entities.Y[1] = 0x3D;

        baron.Step(Slot);

        Assert.Equal(caught ? 0x3A : 0x2E, objects.Type[Slot]);
    }

    // $14A7. A run at zero plans the next: the count steps on, bit 7 flips, and a pause starts.
    [Fact]
    public void PlansTheNextRunAndPauses()
    {
        (ObjectTable objects, _, Baron baron) = Moving();
        objects.EnemyType[Slot] = 0x80;
        objects.Variant[Slot] = 0x03;

        baron.Step(Slot);

        Assert.Equal(0x84, objects.Variant[Slot]);
        Assert.Equal(0x84, objects.EnemyType[Slot]);
        Assert.Equal(0x1D, baron.Waiting[Slot]);
        Assert.Equal(Column, objects.Column[Slot]);
    }

    // $14B6. The count never reaches a multiple of sixteen.
    [Fact]
    public void SkipsACountOfSixteen()
    {
        (ObjectTable objects, _, Baron baron) = Moving();
        objects.EnemyType[Slot] = 0x00;
        objects.Variant[Slot] = 0x8F;

        baron.Step(Slot);

        Assert.Equal(0x0F, objects.Variant[Slot]);
    }

    // $A775 starts at $16 for the first Baron in slot 0.
    [Fact]
    public void WaitsAtFirst()
    {
        (ObjectTable objects, _, Baron baron) = Moving();

        baron.Step(Slot);

        Assert.Equal(0x15, baron.Waiting[Slot]);
        Assert.Equal(Row, objects.Row[Slot]);
    }

    // $14F7. A count that stays positive after the `dec` moves him a row; negative, a column.
    [Theory]
    [InlineData(0x05, Column, Row + 1)]
    [InlineData(0x85, Column + 1, Row)]
    public void MovesOneCellAlongTheRunsAxis(int run, int column, int row)
    {
        (ObjectTable objects, _, Baron baron) = Moving(0);
        objects.EnemyType[Slot] = (byte)run;

        baron.Step(Slot);

        Assert.Equal(column, objects.Column[Slot]);
        Assert.Equal(row, objects.Row[Slot]);
        Assert.Equal(run - 1, objects.EnemyType[Slot]);
        Assert.Equal(0x00, baron.Facing[Slot]);
    }

    // $1523. Arrived in the run's axis, and the run ends where he is.
    [Fact]
    public void EndsTheRunOnArriving()
    {
        (ObjectTable objects, EntityTable entities, Baron baron) = Moving(0);
        objects.EnemyType[Slot] = 0x05;
        entities.Y[Slot] = 0x3D + 0x40;
        objects.Row[Slot] = 0x0D;

        baron.Step(Slot);

        Assert.Equal(0x0D, objects.Row[Slot]);
        Assert.Equal(0x00, objects.EnemyType[Slot]);
    }

    // $14E4. The sprite faces left when the player's column is left of his.
    [Fact]
    public void FacesAPlayerToTheLeft()
    {
        (ObjectTable objects, EntityTable entities, Baron baron) = Moving(0);
        objects.EnemyType[Slot] = 0x85;
        entities.X[Slot] = 0x2C;

        baron.Step(Slot);

        Assert.Equal(0x02, baron.Facing[Slot]);
        Assert.Equal(Column - 1, objects.Column[Slot]);
    }

    // A Baron mid-run at column 10, row 5, and player 1 alive and far away. waited passes clear the
    // $16 the first Baron starts with.
    private static (ObjectTable Objects, EntityTable Entities, Baron Baron) Moving(int waited = -1)
    {
        ObjectTable objects = new();
        EntityTable entities = new();
        Baron baron = new(objects, entities);

        objects.Type[Slot] = 0x2E;
        objects.EnemyType[Slot] = 0x05;
        objects.Column[Slot] = Column;
        objects.Row[Slot] = Row;
        entities.State[Slot] = 0x01;
        entities.X[Slot] = FarX;
        entities.Y[Slot] = FarY;

        if (waited == 0)
        {
            // Twenty-two passes of waiting, each of which leaves the run alone.
            for (int i = 0; i < 0x16; i++)
            {
                baron.Step(Slot);
            }

            objects.Column[Slot] = Column;
            objects.Row[Slot] = Row;
        }

        return (objects, entities, baron);
    }
}

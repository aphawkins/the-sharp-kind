// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
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
        entities.Slot(Slot).State = 0x0E;

        baron.Step(Slot);

        Assert.Equal(0x3A, objects.Slot(Slot).Type);
        Assert.Equal(0x3A, objects.Slot(Slot).EnemyType);
    }

    // $1481-$148F, $1490: when the last Baron goes the clock, which stopped for him, runs again from a full second.
    [Theory]
    [InlineData(0xFF, 0x32)]
    [InlineData(0x2E, 0xFF)]
    [InlineData(0x30, 0xFF)]
    public void TheClockRunsAgainWhenTheLastBaronGoes(byte otherType, byte frames)
    {
        ObjectTable objects = new();
        EntityTable entities = new();
        LevelTimer timer = new();
        Baron baron = new(objects, entities, timer);
        timer.Frames = LevelTimer.Stopped;
        objects.Slot(Slot).Type = 0x2E;
        objects.Slot(Slot ^ 1).Type = otherType;
        entities.Slot(Slot).State = 0x0E;

        baron.Step(Slot);

        Assert.Equal(frames, timer.Frames);
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
        entities.Slot(Slot).X = (byte)x;
        entities.Slot(Slot).Y = (byte)y;

        baron.Step(Slot);

        Assert.Equal(caught ? 0x0E : 0x01, entities.Slot(Slot).State);
        Assert.Equal(caught ? 0x3A : 0x2E, objects.Slot(Slot).Type);
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
        entities.Slot(1).State = (byte)state;
        entities.Slot(1).X = 0x68;
        entities.Slot(1).Y = 0x3D;

        baron.Step(Slot);

        Assert.Equal(caught ? 0x3A : 0x2E, objects.Slot(Slot).Type);
    }

    // $14A7. A run at zero plans the next: the count steps on, bit 7 flips, and a pause starts.
    [Fact]
    public void PlansTheNextRunAndPauses()
    {
        (ObjectTable objects, _, Baron baron) = Moving();
        objects.Slot(Slot).EnemyType = 0x80;
        objects.Slot(Slot).Variant = 0x03;

        baron.Step(Slot);

        Assert.Equal(0x84, objects.Slot(Slot).Variant);
        Assert.Equal(0x84, objects.Slot(Slot).EnemyType);
        Assert.Equal(0x1D, baron.Waiting[Slot]);
        Assert.Equal(Column, objects.Slot(Slot).Column);
    }

    // $14B6. The count never reaches a multiple of sixteen.
    [Fact]
    public void SkipsACountOfSixteen()
    {
        (ObjectTable objects, _, Baron baron) = Moving();
        objects.Slot(Slot).EnemyType = 0x00;
        objects.Slot(Slot).Variant = 0x8F;

        baron.Step(Slot);

        Assert.Equal(0x0F, objects.Slot(Slot).Variant);
    }

    // $A775 starts at $16 for the first Baron in slot 0.
    [Fact]
    public void WaitsAtFirst()
    {
        (ObjectTable objects, _, Baron baron) = Moving();

        baron.Step(Slot);

        Assert.Equal(0x15, baron.Waiting[Slot]);
        Assert.Equal(Row, objects.Slot(Slot).Row);
    }

    // $14F7. A count that stays positive after the `dec` moves him a row; negative, a column.
    [Theory]
    [InlineData(0x05, Column, Row + 1)]
    [InlineData(0x85, Column + 1, Row)]
    public void MovesOneCellAlongTheRunsAxis(int run, int column, int row)
    {
        (ObjectTable objects, _, Baron baron) = Moving(0);
        objects.Slot(Slot).EnemyType = (byte)run;

        baron.Step(Slot);

        Assert.Equal(column, objects.Slot(Slot).Column);
        Assert.Equal(row, objects.Slot(Slot).Row);
        Assert.Equal(run - 1, objects.Slot(Slot).EnemyType);
        Assert.Equal(0x00, baron.Facing[Slot]);
    }

    // $1523. Arrived in the run's axis, and the run ends where he is.
    [Fact]
    public void EndsTheRunOnArriving()
    {
        (ObjectTable objects, EntityTable entities, Baron baron) = Moving(0);
        objects.Slot(Slot).EnemyType = 0x05;
        entities.Slot(Slot).Y = 0x3D + 0x40;
        objects.Slot(Slot).Row = 0x0D;

        baron.Step(Slot);

        Assert.Equal(0x0D, objects.Slot(Slot).Row);
        Assert.Equal(0x00, objects.Slot(Slot).EnemyType);
    }

    // $14E4. The sprite faces left when the player's column is left of his.
    [Fact]
    public void FacesAPlayerToTheLeft()
    {
        (ObjectTable objects, EntityTable entities, Baron baron) = Moving(0);
        objects.Slot(Slot).EnemyType = 0x85;
        entities.Slot(Slot).X = 0x2C;

        baron.Step(Slot);

        Assert.Equal(0x02, baron.Facing[Slot]);
        Assert.Equal(Column - 1, objects.Slot(Slot).Column);
    }

    // A Baron mid-run at column 10, row 5, and player 1 alive and far away. waited passes clear the
    // $16 the first Baron starts with.
    private static (ObjectTable Objects, EntityTable Entities, Baron Baron) Moving(int waited = -1)
    {
        ObjectTable objects = new();
        EntityTable entities = new();
        Baron baron = new(objects, entities, new());

        objects.Slot(Slot).Type = 0x2E;
        objects.Slot(Slot).EnemyType = 0x05;
        objects.Slot(Slot).Column = Column;
        objects.Slot(Slot).Row = Row;
        entities.Slot(Slot).State = 0x01;
        entities.Slot(Slot).X = FarX;
        entities.Slot(Slot).Y = FarY;

        if (waited == 0)
        {
            // Twenty-two passes of waiting, each of which leaves the run alone.
            for (int i = 0; i < 0x16; i++)
            {
                baron.Step(Slot);
            }

            objects.Slot(Slot).Column = Column;
            objects.Slot(Slot).Row = Row;
        }

        return (objects, entities, baron);
    }
}

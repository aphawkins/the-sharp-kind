// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// $EFC0, branch by branch, worked by hand off the reference. Nothing is known to take state 5 yet,
// so there is no capture to replay and these prove the translation against what is written.
//
// The wrap cases lean on SolidMap's own openings. An opening bit clears columns 9 to 12 of its half
// of row 0 or row 24 on the left, and 19 to 22 on the right - $E36E's $87 and $E371's $E1.
public sealed class DiagonalMoverTests
{
    private const int Slot = 2;

    private const byte Left = 0x01;
    private const byte Up = 0x04;

    private const int TopLeftOpen = 0x01;
    private const int BottomLeftOpen = 0x04;

    // Column 7, row 5, both on a boundary.
    private const byte GridX = 0x54;
    private const byte GridY = 0x55;

    // $EF60. At the top, open floor below the thing, and it comes out at the bottom.
    [Fact]
    public void WrapsFromTheTopThroughAnOpenFloor()
    {
        (EntityTable entities, DiagonalMover mover) = Moving(0x55, 0x15, Up);

        mover.Step(Slot, PlayerCell.Of(0x55, 0x15), Map(BottomLeftOpen));

        Assert.Equal(0xF5, entities.Y[Slot]);
        Assert.Equal(Up, entities.Heading[Slot]);
    }

    [Fact]
    public void TurnsAtTheTopOverAClosedFloor()
    {
        (EntityTable entities, DiagonalMover mover) = Moving(0x55, 0x15, Up);

        mover.Step(Slot, PlayerCell.Of(0x55, 0x15), Map(0));

        Assert.Equal(0x15, entities.Y[Slot]);
        Assert.Equal(0x08, entities.Heading[Slot]);
    }

    // $EFAC. At the bottom, open ceiling above the thing, and it comes out at the top.
    [Fact]
    public void WrapsFromTheBottomThroughAnOpenCeiling()
    {
        (EntityTable entities, DiagonalMover mover) = Moving(0x55, 0xF5, 0x00);

        mover.Step(Slot, PlayerCell.Of(0x55, 0xF5), Map(TopLeftOpen));

        Assert.Equal(0x15, entities.Y[Slot]);
        Assert.Equal(0x00, entities.Heading[Slot]);
    }

    // The carry. Both `sbc`s run with it set, so the top takes $13 and the bottom $14, and the two
    // ask about different columns for one X on a column edge. At $53 the top asks about column 9,
    // which is open, and the bottom about column 8, which is not. At $73 it is the other way round:
    // the top asks about 13 and the bottom about 12. A reading that trusts the comments and takes $14
    // at the top fails both.
    [Theory]
    [InlineData(0x53, true, false)]
    [InlineData(0x73, false, true)]
    public void AsksAboutTheColumnAPixelFurtherRightAtTheTop(int column, bool topWraps, bool bottomWraps)
    {
        byte x = (byte)column;

        (EntityTable top, DiagonalMover upward) = Moving(x, 0x15, Up);
        (EntityTable bottom, DiagonalMover downward) = Moving(x, 0xF5, 0x00);

        SolidMap map = Map(TopLeftOpen | BottomLeftOpen);

        upward.Step(Slot, PlayerCell.Of(x, 0x15), map);
        downward.Step(Slot, PlayerCell.Of(x, 0xF5), map);

        Assert.Equal(topWraps ? 0xF5 : 0x15, top.Y[Slot]);
        Assert.Equal(bottomWraps ? 0x15 : 0xF5, bottom.Y[Slot]);
    }

    // $EFA2. Off a row boundary nothing is asked, so a thing past $F5 carries on down.
    [Fact]
    public void DoesNotWrapOffARowBoundary()
    {
        (EntityTable entities, DiagonalMover mover) = Moving(0x55, 0xF6, 0x00);

        mover.Step(Slot, PlayerCell.Of(0x55, 0xF6), Map(TopLeftOpen));

        Assert.Equal(0xF7, entities.Y[Slot]);
    }

    // $EF98. One pixel a pass, and two across.
    [Fact]
    public void MovesOnePixelUpAndTwoAcross()
    {
        (EntityTable entities, DiagonalMover mover) = Moving(GridX, GridY, Up);

        mover.Step(Slot, PlayerCell.Of(GridX, GridY), Map(0));

        Assert.Equal(GridY - 1, entities.Y[Slot]);
        Assert.Equal(GridX + 2, entities.X[Slot]);
    }

    // $EF70. The row above, one and two columns across, turns the thing and leaves it where it is.
    [Fact]
    public void TurnsUnderACeiling()
    {
        (EntityTable entities, DiagonalMover mover) = Moving(GridX, GridY, Up);

        mover.Step(Slot, PlayerCell.Of(GridX, GridY), Map(0, (4, 9)));

        Assert.Equal(GridY, entities.Y[Slot]);
        Assert.Equal(0x08, entities.Heading[Slot]);
    }

    // $EF85. The third cell above only counts when the thing overlaps it.
    [Theory]
    [InlineData(GridX, GridY - 1)]
    [InlineData(GridX + 1, GridY)]
    public void AsksAboutTheThirdCellOnlyOffAColumnBoundary(int x, int expected)
    {
        (EntityTable entities, DiagonalMover mover) = Moving((byte)x, GridY, Up);

        mover.Step(Slot, PlayerCell.Of((byte)x, GridY), Map(0, (4, 10)));

        Assert.Equal(expected, entities.Y[Slot]);
    }

    // $EFBC. Two rows down, and the turn sets the up bit.
    [Fact]
    public void TurnsOnAFloor()
    {
        (EntityTable entities, DiagonalMover mover) = Moving(GridX, GridY, 0x00);

        mover.Step(Slot, PlayerCell.Of(GridX, GridY), Map(0, (7, 8)));

        Assert.Equal(GridY, entities.Y[Slot]);
        Assert.Equal(0x0C, entities.Heading[Slot]);
    }

    // $EEEB. Off a row boundary the third cell to the left counts, and the turn leaves X alone.
    [Fact]
    public void TurnsOffAWallToTheLeft()
    {
        (EntityTable entities, DiagonalMover mover) = Moving(GridX, 0x56, Left);

        mover.Step(Slot, PlayerCell.Of(GridX, 0x56), Map(0, (7, 7)));

        Assert.Equal(GridX, entities.X[Slot]);
        Assert.Equal(0x02, entities.Heading[Slot]);
    }

    // $EF2A. The right probes are three columns across, past the thing's own two.
    [Fact]
    public void TurnsOffAWallToTheRight()
    {
        (EntityTable entities, DiagonalMover mover) = Moving(GridX, GridY, 0x00);

        mover.Step(Slot, PlayerCell.Of(GridX, GridY), Map(0, (5, 10)));

        Assert.Equal(GridX, entities.X[Slot]);
        Assert.Equal(0x03, entities.Heading[Slot]);
    }

    [Fact]
    public void MovesTwoPixelsLeftPastNothing()
    {
        (EntityTable entities, DiagonalMover mover) = Moving(GridX, GridY, Left);

        mover.Step(Slot, PlayerCell.Of(GridX, GridY), Map(0));

        Assert.Equal(GridX - 2, entities.X[Slot]);
    }

    // $EFEA. Every second pass, and the frame counts round four.
    [Fact]
    public void StepsTheFrameEverySecondPass()
    {
        (EntityTable entities, DiagonalMover mover) = Moving(GridX, GridY, 0x00);
        entities.Frame[Slot] = 0x03;
        SolidMap map = Map(0);

        mover.Step(Slot, PlayerCell.Of(entities.X[Slot], entities.Y[Slot]), map);
        Assert.Equal(0x03, entities.Frame[Slot]);

        mover.Step(Slot, PlayerCell.Of(entities.X[Slot], entities.Y[Slot]), map);
        Assert.Equal(0x00, entities.Frame[Slot]);
        Assert.Equal(0x00, entities.AnimationTimer[Slot]);
    }

    private static (EntityTable Entities, DiagonalMover Mover) Moving(byte x, byte y, byte heading)
    {
        EntityTable entities = new();

        entities.X[Slot] = x;
        entities.Y[Slot] = y;
        entities.Heading[Slot] = heading;

        return (entities, new(entities));
    }

    private static SolidMap Map(int wrapOpenings, params (int Row, int Column)[] cells)
    {
        List<string> bitmap = [.. Enumerable.Repeat(new string('.', 32), 23)];

        foreach ((int row, int column) in cells)
        {
            bitmap[row - 1] = $"{bitmap[row - 1][..column]}#{bitmap[row - 1][(column + 1)..]}";
        }

        return SolidMap.Build(new() { Number = 1, WrapOpenings = wrapOpenings, Bitmap = bitmap }, []);
    }
}

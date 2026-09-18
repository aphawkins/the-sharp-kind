// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Bubbles;

// $0E23: two pixels in one of four directions, and the cell bookkeeping with them.
//
// The up arm has a golden trace behind it, taken off the game. The other three are worked by hand
// from the reference, because a bubble on level 1 only ever rises - nothing observed uses them yet.
public sealed class EntityMoverTests
{
    private const int Slot = 17;

    // The rise of one bubble on level 1, read out of VICE a frame at a time: every distinct Y it
    // took, from where it was blown to where it stopped under the ceiling. See docs/bb-port-plan.md.
    //
    // The pauses are not here. The capture shows the same Y on some consecutive frames, because the
    // caller does not run the mover every frame - that cadence belongs to whatever drives it, and
    // this is a test of the mover.
    private static readonly byte[] s_capturedRise =
    [
        0xDD, 0xDB, 0xD9, 0xD7, 0xD5, 0xD3, 0xD1, 0xCF, 0xCD, 0xCB, 0xC9, 0xC7, 0xC5, 0xC3, 0xC1,
        0xBF, 0xBD, 0xBB, 0xB9, 0xB7, 0xB5, 0xB3, 0xB1, 0xAF, 0xAD, 0xAB, 0xA9, 0xA7, 0xA5, 0xA3,
        0xA1, 0x9F, 0x9D, 0x9B, 0x99, 0x97, 0x95, 0x93, 0x91, 0x8F, 0x8D, 0x8B, 0x89, 0x87, 0x85,
        0x83, 0x81, 0x7F, 0x7D, 0x7B, 0x79, 0x77, 0x75, 0x73, 0x71, 0x6F, 0x6D, 0x6B, 0x69, 0x67,
        0x65, 0x63, 0x61, 0x5F, 0x5D, 0x5B, 0x59, 0x57, 0x55, 0x53, 0x51, 0x4F, 0x4D,
    ];

    // The whole captured rise, step for step. A mover that moved one pixel, or two on alternate
    // calls, would pass a test of the first step and fail here on the second.
    [Fact]
    public void ReproducesTheCapturedRise()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, s_capturedRise[0]);
        byte[] actual = new byte[s_capturedRise.Length];
        actual[0] = objects.Y[Slot];

        for (int step = 1; step < s_capturedRise.Length; step++)
        {
            mover.Step(Slot, EntityMover.Up);
            actual[step] = objects.Y[Slot];
        }

        Assert.Equal(s_capturedRise, actual);
    }

    // And the horizontal byte is untouched across all of it, which the capture also shows: X stayed
    // at $74 for the whole rise.
    [Fact]
    public void LeavesTheHorizontalByteAloneWhileRising()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0xDD);

        for (int step = 1; step < s_capturedRise.Length; step++)
        {
            mover.Step(Slot, EntityMover.Up);
        }

        Assert.Equal(0x74, objects.X[Slot]);
    }

    // $0E33's mask. Two below zero is $FE, and masked to three bits it is six - so the sub-position
    // runs 0, 6, 4, 2, 0 and the row steps on every fourth call.
    [Fact]
    public void StepsTheRowEveryFourthCallGoingUp()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0xDD);
        objects.Row[Slot] = 0x19;

        byte[] expected = [6, 4, 2, 0];

        for (int step = 0; step < 4; step++)
        {
            mover.Step(Slot, EntityMover.Up);
            Assert.Equal(expected[step], objects.SubY[Slot]);
            Assert.Equal(step == 3 ? 0x18 : 0x19, objects.Row[Slot]);
        }
    }

    // $0E3D. Off the top of the playfield and back on at the bottom.
    [Fact]
    public void WrapsFromTheTopRowToTheBottom()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0x20);
        objects.Row[Slot] = 0x00;
        objects.SubY[Slot] = 0x02;

        mover.Step(Slot, EntityMover.Up);

        Assert.Equal(0x1A, objects.Row[Slot]);
    }

    // $0E41. A thing that wraps becomes type $38, unless it is already a high type.
    [Fact]
    public void RetypesAWrappedThing()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0x20);
        objects.Row[Slot] = 0x00;
        objects.SubY[Slot] = 0x02;
        objects.Type[Slot] = 0x04;

        mover.Step(Slot, EntityMover.Up);

        Assert.Equal(0x38, objects.Type[Slot]);
    }

    [Fact]
    public void LeavesAHighTypeAloneOnWrapping()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0x20);
        objects.Row[Slot] = 0x00;
        objects.SubY[Slot] = 0x02;
        objects.Type[Slot] = 0x1A;

        mover.Step(Slot, EntityMover.Up);

        Assert.Equal(0x1A, objects.Type[Slot]);
    }

    // $0E75. Down is two pixels as well.
    [Fact]
    public void MovesTwoPixelsDown()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0x40);

        mover.Step(Slot, EntityMover.Down);

        Assert.Equal(0x42, objects.Y[Slot]);
        Assert.Equal(0x02, objects.SubY[Slot]);
    }

    // $0E90, and the asymmetry worth having a test of its own. Going down the row steps when the
    // sub-position reaches two, not when it wraps at eight - one step later than the up arm's rule.
    // A mover written as a mirror of the up arm fails exactly here.
    [Fact]
    public void StepsTheRowAtTwoGoingDownRatherThanAtTheWrap()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0x40);
        objects.Row[Slot] = 0x10;
        objects.SubY[Slot] = 0x06;

        // Six to eight: the sub-position wraps and the row does not move.
        mover.Step(Slot, EntityMover.Down);

        Assert.Equal(0x00, objects.SubY[Slot]);
        Assert.Equal(0x10, objects.Row[Slot]);

        // Zero to two: the row moves now.
        mover.Step(Slot, EntityMover.Down);

        Assert.Equal(0x02, objects.SubY[Slot]);
        Assert.Equal(0x11, objects.Row[Slot]);
    }

    // $0E9A. Off the bottom and back on at the top.
    [Fact]
    public void WrapsFromTheBottomRowToTheTop()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0x40);
        objects.Row[Slot] = 0x1C;
        objects.SubY[Slot] = 0x00;
        objects.Type[Slot] = 0x04;

        mover.Step(Slot, EntityMover.Down);

        Assert.Equal(0x00, objects.Row[Slot]);
        Assert.Equal(0x38, objects.Type[Slot]);
    }

    // $0E56. Two pixels, because $0E52's compare leaves the carry set and the add picks it up.
    // Reading that `adc #$01` as one pixel is the easiest mistake in the whole routine.
    [Fact]
    public void MovesTwoPixelsRight()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0x40);

        mover.Step(Slot, EntityMover.Right);

        Assert.Equal(0x76, objects.X[Slot]);
        Assert.Equal(0x01, objects.SubX[Slot]);
    }

    // $0E5F. Four quarter-cells to a column, which is the eight pixels a column is wide.
    [Fact]
    public void StepsTheColumnEveryFourthCallGoingRight()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0x40);
        objects.Column[Slot] = 0x0C;

        for (int step = 0; step < 3; step++)
        {
            mover.Step(Slot, EntityMover.Right);
        }

        Assert.Equal(0x03, objects.SubX[Slot]);
        Assert.Equal(0x0C, objects.Column[Slot]);

        mover.Step(Slot, EntityMover.Right);

        Assert.Equal(0x00, objects.SubX[Slot]);
        Assert.Equal(0x0D, objects.Column[Slot]);
        Assert.Equal(0x7C, objects.X[Slot]);
    }

    // $0EAD, the mirror of the right arm - and this one really is a mirror.
    [Fact]
    public void MovesTwoPixelsLeft()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0x40);
        objects.SubX[Slot] = 0x02;

        mover.Step(Slot, EntityMover.Left);

        Assert.Equal(0x72, objects.X[Slot]);
        Assert.Equal(0x01, objects.SubX[Slot]);
    }

    // $0EB4. The sub-position borrows into the column below.
    [Fact]
    public void StepsTheColumnDownGoingLeft()
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0x40);
        objects.Column[Slot] = 0x0C;
        objects.SubX[Slot] = 0x00;

        mover.Step(Slot, EntityMover.Left);

        Assert.Equal(0x03, objects.SubX[Slot]);
        Assert.Equal(0x0B, objects.Column[Slot]);
    }

    // $0E23's own mask. The reference takes a whole byte and uses its low two bits, so a caller
    // holding a wider value reaches the arm those bits name.
    [Theory]
    [InlineData(0x04, 0x40 - 2)]
    [InlineData(0x86, 0x40 + 2)]
    public void MasksTheDirectionToTwoBits(int direction, int expectedY)
    {
        (ObjectTable objects, EntityMover mover) = Bubble(0x74, 0x40);

        mover.Step(Slot, direction);

        Assert.Equal(expectedY, objects.Y[Slot]);
    }

    private static (ObjectTable Objects, EntityMover Mover) Bubble(byte x, byte y)
    {
        ObjectTable objects = new();

        objects.Type[Slot] = BubbleBlow.BubbleType;
        objects.X[Slot] = x;
        objects.Y[Slot] = y;

        return (objects, new EntityMover(objects));
    }
}

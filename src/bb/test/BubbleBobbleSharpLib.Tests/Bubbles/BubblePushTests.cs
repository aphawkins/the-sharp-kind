// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Bubbles;

// $0AAB, worked by hand off the reference. Player 1 stands at Y $80; the bubble is in slot 10, on
// row 10 (SolidMap row 6), eight pixels from the player on the side being tested.
public sealed class BubblePushTests
{
    private const int Slot = 10;
    private const byte Row = 10;
    private const byte Column = 5;

    // $0B83 and $0B8B. Four pixels right, and the sub-position 1 + 2 = 3 stays in the column.
    [Fact]
    public void FacingRightPushesABubbleRight()
    {
        (EntityTable entities, ObjectTable objects) = Rig(frame: 0x00, playerX: 0x40, bubbleX: 0x48, subX: 1);

        new BubblePush(entities, objects).Update(Open());

        Assert.Equal(0x4C, objects.X[Slot]);
        Assert.Equal(3, objects.SubX[Slot]);
        Assert.Equal(Column, objects.Column[Slot]);
    }

    // $0B90 to $0B97. 2 + 2 wraps to 0, which is smaller, so the bubble moves into the next column.
    [Fact]
    public void ARightPushCanCrossAColumn()
    {
        (EntityTable entities, ObjectTable objects) = Rig(frame: 0x00, playerX: 0x40, bubbleX: 0x48, subX: 2);

        new BubblePush(entities, objects).Update(Open());

        Assert.Equal(0, objects.SubX[Slot]);
        Assert.Equal(Column + 1, objects.Column[Slot]);
    }

    // $0AF2 to $0B05. The `sbc #$03` runs with the carry clear, so four pixels; 1 - 2 borrows.
    [Fact]
    public void FacingLeftPushesABubbleLeft()
    {
        (EntityTable entities, ObjectTable objects) = Rig(frame: 0x04, playerX: 0x50, bubbleX: 0x48, subX: 1);

        new BubblePush(entities, objects).Update(Open());

        Assert.Equal(0x44, objects.X[Slot]);
        Assert.Equal(3, objects.SubX[Slot]);
        Assert.Equal(Column - 1, objects.Column[Slot]);
    }

    // $0B3B. Pushed into a wall one column left, and put back: $44 - $14 + 7 + 1, masked, + $14 + 0.
    [Fact]
    public void ALeftPushIntoAWallSnapsBack()
    {
        (EntityTable entities, ObjectTable objects) = Rig(frame: 0x04, playerX: 0x50, bubbleX: 0x48, subX: 0);

        new BubblePush(entities, objects).Update(Solid(Row - 4 + 1, Column - 1));

        Assert.Equal(0x4C, objects.X[Slot]);
        Assert.Equal(0, objects.SubX[Slot]);
        Assert.Equal(Column, objects.Column[Slot]);
    }

    // $0BD4. The right arm reads one column across: $4C - $1C, masked, + $13 + 1.
    [Fact]
    public void ARightPushIntoAWallSnapsBack()
    {
        (EntityTable entities, ObjectTable objects) = Rig(frame: 0x00, playerX: 0x40, bubbleX: 0x48, subX: 2);

        new BubblePush(entities, objects).Update(Solid(Row - 4 + 1, Column + 2));

        Assert.Equal(0x44, objects.X[Slot]);
        Assert.Equal(0, objects.SubX[Slot]);
        Assert.Equal(Column, objects.Column[Slot]);
    }

    // $0AD6 and $0B68 are one-sided, $0AB7 stops frames 8 up, and $0AC7 a slot with a state byte.
    [Theory]
    [InlineData(0x00, 0x50, 0x48, 0x00)]
    [InlineData(0x04, 0x40, 0x48, 0x00)]
    [InlineData(0x08, 0x40, 0x48, 0x00)]
    [InlineData(0x00, 0x40, 0x52, 0x00)]
    [InlineData(0x00, 0x40, 0x48, 0x01)]
    public void NothingElseIsPushed(byte frame, byte playerX, byte bubbleX, byte state)
    {
        (EntityTable entities, ObjectTable objects) = Rig(frame, playerX, bubbleX, subX: 1);
        objects.State[Slot] = state;

        new BubblePush(entities, objects).Update(Open());

        Assert.Equal(bubbleX, objects.X[Slot]);
        Assert.Equal(1, objects.SubX[Slot]);
    }

    // $0B0E. Row 3 leaves Y at 3; `dey` then walks 2, 1 and 0, so player 1 pushes the same bubble
    // again, and again, until it is $14 away: four pushes, and two columns.
    [Fact]
    public void ALeftPushOffTheMapPushesUntilOutOfRange()
    {
        (EntityTable entities, ObjectTable objects) = Rig(frame: 0x04, playerX: 0x50, bubbleX: 0x4C, subX: 0);
        objects.Row[Slot] = 0x03;
        objects.Column[Slot] = 8;

        new BubblePush(entities, objects).Update(Open());

        Assert.Equal(0x3C, objects.X[Slot]);
        Assert.Equal(0, objects.SubX[Slot]);
        Assert.Equal(6, objects.Column[Slot]);
    }

    // $0B0E. With Y at 3, the slots still to come are measured from entity 3.
    [Fact]
    public void ALeftPushOffTheMapMakesEntityThreeThePusher()
    {
        (EntityTable entities, ObjectTable objects) = Rig(frame: 0x04, playerX: 0x50, bubbleX: 0x40, subX: 1);
        objects.X[Slot] = 0x4C;
        objects.Row[Slot] = 0x03;

        const int Lower = 4;
        objects.Type[Lower] = 0x00;
        objects.X[Lower] = 0x98;
        objects.Y[Lower] = 0x60;
        objects.SubX[Lower] = 1;
        objects.Column[Lower] = 15;
        objects.Row[Lower] = Row;
        entities.X[3] = 0xA0;
        entities.Y[3] = 0x60;

        new BubblePush(entities, objects).Update(Open());

        // Player 1 pushes slot 17 three times from row 3, and each time entity 3 then pushes this
        // one, until it is $14 from entity 3.
        Assert.Equal(0x8C, objects.X[Lower]);
    }

    // Row $40 walks Y down through zero page. At $0A, $BC (entity 2's X) reads as a playing state,
    // and its frame is at $852A, past the eight the port keeps: a named gap.
    [Fact]
    public void AnIndexPastTheTablesIsAGap()
    {
        (EntityTable entities, ObjectTable objects) = Rig(frame: 0x04, playerX: 0x50, bubbleX: 0x4C, subX: 1);
        objects.Row[Slot] = 0x40;
        entities.X[2] = 0x01;

        Assert.Throws<NotSupportedException>(() => new BubblePush(entities, objects).Update(Open()));
    }

    private static (EntityTable Entities, ObjectTable Objects) Rig(byte frame, byte playerX, byte bubbleX, byte subX)
    {
        EntityTable entities = new();
        entities.State[0] = PlayerFrame.PlayingState;
        entities.Frame[0] = frame;
        entities.X[0] = playerX;
        entities.Y[0] = 0x80;

        ObjectTable objects = new();
        objects.Type[Slot] = 0x00;
        objects.X[Slot] = bubbleX;
        objects.Y[Slot] = 0x80;
        objects.SubX[Slot] = subX;
        objects.Column[Slot] = Column;
        objects.Row[Slot] = Row;

        return (entities, objects);
    }

    private static SolidMap Open() => SolidMap.Build(Level(), []);

    private static SolidMap Solid(int row, int column) => SolidMap.Build(Level((row, column)), []);

    // SolidMap puts the level's own rows below a solid ceiling, so a map row is one more than the line.
    private static Level Level(params (int Row, int Column)[] cells)
    {
        List<string> bitmap = [.. Enumerable.Repeat(new string('.', 32), 23)];

        foreach ((int row, int column) in cells)
        {
            bitmap[row - 1] = $"{bitmap[row - 1][..column]}#{bitmap[row - 1][(column + 1)..]}";
        }

        return new() { Number = 1, Bitmap = bitmap };
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Bubbles;

// $0E23 in enemy-ai.s: two pixels in one of four directions, and the cell bookkeeping that goes
// with them.
//
// It is not a bubble routine and it is not in any of the files Phase 5 names. It is the mover the
// whole entity table shares, reached from the enemy AI loop at $0CF2, and a bubble rising is one
// caller of it. It is translated here because the bubble is the first thing to need it; Phase 6
// will find it already done.
//
// **Found with VICE, not by reading.** A store watchpoint on slot 17's Y byte traps twice a frame
// while a bubble rises, at $0E30 and $0E33, which are the two `dec D_AA1E,x` of the up arm. That
// is how a mover whose four arms are buried in the middle of the enemy AI was found at all.
//
// Two pixels, not eight. The eight-pixel-a-frame shot that a bubble opens with is a different
// mover, in the dispatcher at $0F98, and it is not this. This is the slow half: the rise, and the
// drift at the top.
internal sealed class EntityMover
{
    // The four arms of $0E23, picked by the low two bits of A.
    internal const int Up = 0;
    internal const int Right = 1;
    internal const int Down = 2;
    internal const int Left = 3;

    // $0E39 and $0E95. A row off either end of the playfield comes back at the other, which is the
    // vertical wrap the level data calls an opening.
    private const byte BottomRow = 0x1A;
    private const byte RowCount = 0x1D;

    // $0E45. A thing that wraps and is not already one of the high types becomes $38.
    private const byte WrapBelow = 0x18;
    private const byte WrapType = 0x38;

    // $0E5F and $0EB4. The horizontal sub-position counts quarters of a cell, so four of them are
    // the eight pixels a column is wide.
    private const byte SubXCount = 0x04;

    private readonly ObjectTable _objects;

    internal EntityMover(ObjectTable objects)
    {
        ArgumentNullException.ThrowIfNull(objects);

        _objects = objects;
    }

    // $0E23. The reference takes the direction in A and masks it, so a caller holding a wider byte
    // reaches the arm the low two bits name.
    internal void Step(int slot, int direction)
    {
        switch (direction & 0x03)
        {
            case Up:
                MoveUp(slot);
                break;
            case Right:
                MoveRight(slot);
                break;
            case Down:
                MoveDown(slot);
                break;
            default:
                MoveLeft(slot);
                break;
        }
    }

    // $0E27. Two pixels up, and the row steps when the sub-position comes back to zero.
    private void MoveUp(int slot)
    {
        _objects.SubY[slot] -= 2;
        _objects.Y[slot] -= 2;

        // $0E33's `and #$07`, which is what turns the sub-position's borrow into a wrap: two below
        // zero is $FE, and masked it is six.
        _objects.SubY[slot] &= 0x07;

        if (_objects.SubY[slot] != 0)
        {
            return;
        }

        _objects.Row[slot]--;

        // $0E3D. The row is a byte and the test is on its top bit, so the row before zero is $FF.
        if ((_objects.Row[slot] & 0x80) == 0)
        {
            return;
        }

        _objects.Row[slot] = BottomRow;
        Wrapped(slot);
    }

    // $0E75. Two pixels down - and the row steps a sub-position *earlier* than the up arm's does.
    //
    // That is the reference's own asymmetry, not a slip here. Going up, $0E36 steps the row when
    // the sub-position reaches zero. Going down, $0E90 steps it when the sub-position reaches two,
    // one step after the wrap at eight rather than on it. A mover written as a mirror of the up arm
    // puts every downward thing a row out for one step in eight.
    private void MoveDown(int slot)
    {
        _objects.SubY[slot] += 2;
        _objects.Y[slot] += 2;

        if (_objects.SubY[slot] == 0x08)
        {
            _objects.SubY[slot] = 0;
            return;
        }

        if (_objects.SubY[slot] != 0x02)
        {
            return;
        }

        _objects.Row[slot]++;

        if (_objects.Row[slot] != RowCount)
        {
            return;
        }

        _objects.Row[slot] = 0;
        Wrapped(slot);
    }

    // $0E55. Two pixels right, not one: $0E52's `cmp #$01` falls through with the carry set, so the
    // `adc #$01` at $0E56 adds two. Reading it as one pixel gives a mover that agrees with the left
    // arm's `sbc #$02` in neither speed nor symmetry.
    private void MoveRight(int slot)
    {
        _objects.X[slot] += 2;
        _objects.SubX[slot]++;

        if (_objects.SubX[slot] != SubXCount)
        {
            return;
        }

        _objects.SubX[slot] = 0;
        _objects.Column[slot]++;

        // $0E6F's `bne L0EC2` falls through into the down arm when the column wraps to zero. A
        // column is 0 to 31 and nothing can carry it past 255, so that fall-through is unreachable
        // and is not reproduced.
    }

    // $0EAD. Two pixels left, and the sub-position borrows into the column below.
    private void MoveLeft(int slot)
    {
        _objects.X[slot] -= 2;
        _objects.SubX[slot]--;

        if ((_objects.SubX[slot] & 0x80) == 0)
        {
            return;
        }

        _objects.SubX[slot] = SubXCount - 1;
        _objects.Column[slot]--;
    }

    // $0E41 and $0E9E, which are the same three instructions twice. A thing that has just wrapped
    // off one end of the playfield becomes type $38, unless it is already a high type.
    private void Wrapped(int slot)
    {
        if (_objects.Type[slot] >= WrapBelow)
        {
            return;
        }

        _objects.Type[slot] = WrapType;
    }
}

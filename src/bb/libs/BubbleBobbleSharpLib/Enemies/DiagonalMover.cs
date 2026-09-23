// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $EFC0 in entity-system.s: state 5 of the $1E3A table, a thing that travels on the diagonal and
// turns round off whatever it meets.
//
// One pixel up or down and two across, every pass. $85E8 holds which way, and a solid cell turns
// only the axis that met it, so the thing bounces round the level rather than stopping. Which enemy
// takes state 5 is not known yet - nothing translated so far writes it.
//
// **This is the level's wrapOpenings at work.** $EF4C and $EFA0, the two vertical halves, are the
// only code in the reference that reads the wrap tables at $8501 and $88C1, and those are SolidMap's
// row 0 and row 24, which init_level_renderer opens per wrapOpenings. A thing at the top of the
// playfield comes back at the bottom when the floor under it is open, and turns round when it is
// not. EntityMover's $0E23 wraps a row without asking anything.
//
// **The two wrap checks are a pixel apart.** Both reference comments say the `sbc` runs with the
// carry clear, and in both cases it is set: the top test is reached through a `bne` not taken, so
// the `cmp #$15` was equal, and the bottom test through a `bcc` not taken. So `sbc #$13` takes $13
// and `sbc #$14` takes $14, and a thing at the top asks about the column a pixel to the right of the
// one a thing at the bottom asks about.
//
// $EEEB and $EF2A, the horizontal halves, have other callers that store $BD into $EF15 and so also
// flip the sprite's facing when they turn. $EFD6 stores $60, an `rts`, and that is the only path
// here.
internal sealed class DiagonalMover
{
    // $85E8. Bit 0 set is left and bit 2 set is up.
    private const byte LeftBit = 0x01;
    private const byte UpBit = 0x04;

    // $EF0D and $EF8F. Each turn flips its axis's pair of bits.
    private const byte TurnAcross = 0x03;
    private const byte TurnUpOrDown = 0x0C;

    // $EFC0 puts one in $04, and $EF4C negates it for the up arm.
    private const byte Down = 0x01;
    private const byte Up = 0xFF;

    // $EF08 and $EF47. Two pixels, by two `dec` or two `inc`.
    private const byte Across = 0x02;

    // $EF5A and $EFA6. The top test is equality and the bottom one is not.
    private const byte TopWrap = 0x15;
    private const byte BottomWrap = 0xF5;

    // $EF60 and $EFAC, with the carry each really has.
    private const byte TopWrapOffset = 0x13;
    private const byte BottomWrapOffset = 0x14;

    // $EF66 and $EFB2: $88C1 and $8501, one column into the floor and the ceiling.
    private const int FloorRow = SolidMap.Rows - 1;
    private const int CeilingRow = 0;
    private const int WrapColumnBias = 1;

    // $EF7B, read one row up from the cell $1E6C found.
    private const int AboveLeft = 0x01;
    private const int AboveMiddle = 0x02;
    private const int AboveRight = 0x03;

    // $EFBC. Two rows down, and into the same loop at $EF7D.
    private const int BelowLeft = 0x51;
    private const int BelowMiddle = 0x52;
    private const int BelowRight = 0x53;

    // $EEEB and $EF2A.
    private const int LeftTop = 0x00;
    private const int LeftMiddle = 0x28;
    private const int LeftBottom = 0x50;
    private const int RightTop = 0x03;
    private const int RightMiddle = 0x2B;
    private const int RightBottom = 0x53;

    // $EFEA. Every second pass, and four frames.
    private const byte AnimationPeriod = 0x02;
    private const byte FrameMask = 0x03;

    private readonly EntityTable _entities;

    internal DiagonalMover(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities;
    }

    // $EFC0. The cell is the one $1E6C found before it dispatched here, and it serves the vertical
    // half only: $EFD6 calls $E9B8 again before the horizontal half, so that probes from where the
    // thing has just moved to.
    internal void Step(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        // $EFC7. Saved before the vertical half can turn it, and the horizontal half reads this.
        byte heading = _entities.Heading[slot];

        if ((heading & UpBit) != 0)
        {
            StepUp(slot, cell, map);
        }
        else
        {
            StepDown(slot, cell, map);
        }

        PlayerCell moved = PlayerCell.Of(_entities.X[slot], _entities.Y[slot]);

        if ((heading & LeftBit) != 0)
        {
            StepLeft(slot, moved, map);
        }
        else
        {
            StepRight(slot, moved, map);
        }

        Animate(slot);
    }

    // $EF7D. The third cell only counts when the thing is off a column boundary and so overlaps it.
    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left) || cell.Solid(map, middle) || (cell.FineX != 0 && cell.Solid(map, right));

    // $EEEB and $EF2A. The third cell only counts when the thing is off a row boundary.
    private static bool Beside(in PlayerCell cell, SolidMap map, int top, int middle, int bottom)
        => cell.Solid(map, top) || cell.Solid(map, middle) || (cell.FineY != 0 && cell.Solid(map, bottom));

    // $EF4C. Off a row boundary nothing is probed at all.
    private void StepUp(int slot, in PlayerCell cell, SolidMap map)
    {
        if (cell.FineY != 0)
        {
            MoveVertically(slot, Up);
            return;
        }

        byte y = _entities.Y[slot];

        if (y == TopWrap)
        {
            WrapThrough(slot, map, FloorRow, TopWrapOffset, BottomWrap);
            return;
        }

        // $EF70 takes forty off the pointer, which is the row above.
        PlayerCell above = cell with { Row = cell.Row - 1 };

        if (Blocked(above, map, AboveLeft, AboveMiddle, AboveRight))
        {
            TurnVertically(slot);
            return;
        }

        MoveVertically(slot, Up);
    }

    // $EFA0.
    private void StepDown(int slot, in PlayerCell cell, SolidMap map)
    {
        if (cell.FineY != 0)
        {
            MoveVertically(slot, Down);
            return;
        }

        if (_entities.Y[slot] >= BottomWrap)
        {
            WrapThrough(slot, map, CeilingRow, BottomWrapOffset, TopWrap);
            return;
        }

        if (Blocked(cell, map, BelowLeft, BelowMiddle, BelowRight))
        {
            TurnVertically(slot);
            return;
        }

        MoveVertically(slot, Down);
    }

    // $EF5E and $EFAA. The cell on the far edge is the whole question: open, and the thing comes
    // out at the other end without moving further this pass; solid, and it turns round where it is.
    private void WrapThrough(int slot, SolidMap map, int row, byte offset, byte arrival)
    {
        int column = WrapColumnBias + (unchecked((byte)(_entities.X[slot] - offset)) >> 3);

        if (map[row, column])
        {
            TurnVertically(slot);
            return;
        }

        _entities.Y[slot] = arrival;
    }

    // $EEEB. On a column boundary the three cells to the left are probed, the third only when the
    // thing is off a row boundary.
    private void StepLeft(int slot, in PlayerCell cell, SolidMap map)
    {
        if (cell.FineX == 0 && Beside(cell, map, LeftTop, LeftMiddle, LeftBottom))
        {
            _entities.Heading[slot] ^= TurnAcross;
            return;
        }

        _entities.X[slot] = unchecked((byte)(_entities.X[slot] - Across));
    }

    // $EF2A.
    private void StepRight(int slot, in PlayerCell cell, SolidMap map)
    {
        if (cell.FineX == 0 && Beside(cell, map, RightTop, RightMiddle, RightBottom))
        {
            _entities.Heading[slot] ^= TurnAcross;
            return;
        }

        _entities.X[slot] = unchecked((byte)(_entities.X[slot] + Across));
    }

    // $EF98. $04 is added, so the up arm's $FF is a byte add that wraps.
    private void MoveVertically(int slot, byte step)
        => _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] + step));

    // $EF8F.
    private void TurnVertically(int slot) => _entities.Heading[slot] ^= TurnUpOrDown;

    // $EFEA. Reached whatever the two halves did, including a wrap and a turn.
    private void Animate(int slot)
    {
        _entities.AnimationTimer[slot]++;

        if (_entities.AnimationTimer[slot] < AnimationPeriod)
        {
            return;
        }

        _entities.AnimationTimer[slot] = 0;
        _entities.Frame[slot] = (byte)((_entities.Frame[slot] + 1) & FrameMask);
    }
}

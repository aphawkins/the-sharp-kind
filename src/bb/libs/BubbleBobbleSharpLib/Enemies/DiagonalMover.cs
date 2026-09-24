// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $EFC0 and $EEB2 in entity-system.s: states 5 and 6 of the $1E3A table, things that travel on the
// diagonal and turn round off whatever they meet. The spawn gives class 3 state 5 and class 4 state 6.
//
// One pixel up or down and two across, every pass - two up or down for state 6. $85E8 holds which
// way, and a solid cell turns only the axis that met it, so the thing bounces round the level rather
// than stopping.
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
// $EEEB and $EF2A, the horizontal halves, are entered with $60, an `rts`, in $EF15 by state 5, and
// with $BD by state 6, which makes a turn flip the sprite's facing too.
internal sealed class DiagonalMover
{
    // $85E8. Bit 0 set is left and bit 2 set is up.
    private const byte LeftBit = 0x01;
    private const byte UpBit = 0x04;

    // $EF0D and $EF8F. Each turn flips its axis's pair of bits.
    private const byte TurnAcross = 0x03;
    private const byte TurnUpOrDown = 0x0C;

    // $EFC0 puts one in $04 and $EEB2 puts two, and $EF4C negates it for the up arm.
    private const byte Slow = 0x01;
    private const byte Fast = 0x02;

    // $EEDF and $EF1E. State 6 faces right on frames 0 to 3 and left from 4.
    private const byte FacingLeft = 0x04;

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
        PlayerCell moved = Vertically(slot, heading, cell, map, Slow);

        if ((heading & LeftBit) != 0)
        {
            StepLeft(slot, moved, map, false);
        }
        else
        {
            StepRight(slot, moved, map, false);
        }

        Animate(slot);
    }

    // $EEB2, state 6: the same diagonal at twice the vertical speed, by a thing that faces the way it
    // goes. It turns to face first and moves on the next pass, and a wall that turns it round also
    // flips its frame, because $EEEB and $EF2A are entered with $BD in $EF15 rather than an `rts`.
    // Then the frame steps through $EB0F, not $EFEA.
    internal void StepFacing(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        // $1E6F's copy of the frame, which $EEDF and $EF1E read.
        byte facing = _entities.Frame[slot];
        byte heading = _entities.Heading[slot];
        PlayerCell moved = Vertically(slot, heading, cell, map, Fast);

        if ((heading & LeftBit) != 0)
        {
            if (facing < FacingLeft)
            {
                _entities.Frame[slot] = FacingLeft;
            }
            else
            {
                StepLeft(slot, moved, map, true);
            }
        }
        else if (facing >= FacingLeft)
        {
            _entities.Frame[slot] = 0;
        }
        else
        {
            StepRight(slot, moved, map, true);
        }

        EntityAnimation.Step(_entities, slot);
    }

    // $EFEA. Reached whatever the two halves did, including a wrap and a turn. $1E87 calls it on its
    // own for a thing that is not free to move.
    internal void Animate(int slot)
    {
        _entities.AnimationTimer[slot]++;

        if (_entities.AnimationTimer[slot] < AnimationPeriod)
        {
            return;
        }

        _entities.AnimationTimer[slot] = 0;
        _entities.Frame[slot] = (byte)((_entities.Frame[slot] + 1) & FrameMask);
    }

    // $EF7D. The third cell only counts when the thing is off a column boundary and so overlaps it.
    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left) || cell.Solid(map, middle) || (cell.FineX != 0 && cell.Solid(map, right));

    // $EEEB and $EF2A. The third cell only counts when the thing is off a row boundary.
    private static bool Beside(in PlayerCell cell, SolidMap map, int top, int middle, int bottom)
        => cell.Solid(map, top) || cell.Solid(map, middle) || (cell.FineY != 0 && cell.Solid(map, bottom));

    // $EFC7 to $EFD6, and $EEBB to $EEC8: the vertical half, then the cell again from where it left
    // the thing.
    private PlayerCell Vertically(int slot, byte heading, in PlayerCell cell, SolidMap map, byte speed)
    {
        if ((heading & UpBit) != 0)
        {
            StepUp(slot, cell, map, unchecked((byte)-speed));
        }
        else
        {
            StepDown(slot, cell, map, speed);
        }

        return PlayerCell.Of(_entities.X[slot], _entities.Y[slot]);
    }

    // $EF4C. Off a row boundary nothing is probed at all.
    private void StepUp(int slot, in PlayerCell cell, SolidMap map, byte up)
    {
        if (cell.FineY != 0)
        {
            MoveVertically(slot, up);
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

        MoveVertically(slot, up);
    }

    // $EFA0.
    private void StepDown(int slot, in PlayerCell cell, SolidMap map, byte down)
    {
        if (cell.FineY != 0)
        {
            MoveVertically(slot, down);
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

        MoveVertically(slot, down);
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
    private void StepLeft(int slot, in PlayerCell cell, SolidMap map, bool faces)
    {
        if (cell.FineX == 0 && Beside(cell, map, LeftTop, LeftMiddle, LeftBottom))
        {
            TurnAcrossWay(slot, faces);
            return;
        }

        _entities.X[slot] = unchecked((byte)(_entities.X[slot] - Across));
    }

    // $EF2A.
    private void StepRight(int slot, in PlayerCell cell, SolidMap map, bool faces)
    {
        if (cell.FineX == 0 && Beside(cell, map, RightTop, RightMiddle, RightBottom))
        {
            TurnAcrossWay(slot, faces);
            return;
        }

        _entities.X[slot] = unchecked((byte)(_entities.X[slot] + Across));
    }

    // $EF98. $04 is added, so the up arm's $FF is a byte add that wraps.
    private void MoveVertically(int slot, byte step)
        => _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] + step));

    // $EF8F.
    private void TurnVertically(int slot) => _entities.Heading[slot] ^= TurnUpOrDown;

    // $EF0D, and $EF15 when it holds $BD: `lda D_8520,x` / `eor #$04` / `sta D_8520,x`.
    private void TurnAcrossWay(int slot, bool faces)
    {
        _entities.Heading[slot] ^= TurnAcross;

        if (faces)
        {
            _entities.Frame[slot] ^= FacingLeft;
        }
    }
}

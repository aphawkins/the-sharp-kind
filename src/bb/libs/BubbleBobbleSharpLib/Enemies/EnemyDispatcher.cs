// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $0F48, $0F61, $0F91, $0F98 and $100B in enemy-ai.s, with $7BFE from level-data-part2.s folded in:
// what the AI loop does with a slot that already has an AI state.
//
// This is the eight-pixel half of entity movement. The slow half - two pixels, four directions - is
// EntityMover at $0E23, and the two are not variants of one another. A bubble opens by travelling
// eight pixels a frame through here, and then rises through EntityMover.
//
// The bubble collision at $105B is BubbleCollision, called down both arms of $0F98.
internal sealed class EnemyDispatcher
{
    // The six probe offsets of $0F98, written as the reference writes them: an offset off a pointer
    // into a map forty bytes to the row. $28 is the row below, $50 two rows below, and the odd ones
    // are the same cells one column across.
    private const int Cell = 0x00;
    private const int CellRight = 0x01;
    private const int Below = 0x28;
    private const int BelowRight = 0x29;
    private const int TwoBelow = 0x50;
    private const int TwoBelowRight = 0x51;

    private const int Stride = 40;

    // An entity's stored row is four greater than the row of SolidMap, and every probe here takes
    // those four off.
    //
    // The four is right but not for the obvious reason, and the obvious reason is wrong. $8500 is
    // NOT the first row of level tiles: sprites2-tables.s lays each forty-byte group out as eight
    // bytes of entity metadata followed by thirty-two tile bytes, and $8500-$851F is the screen wrap
    // permission table and the two spawn point availability tables. Tile rows start at $8528, which
    // is $AC01 entry five rather than four.
    //
    // What rescues the four is that the wrap permission block doubles as the ceiling a probe hits -
    // it reads solid - so it is SolidMap's row 0. That gives SolidMap row = tile row + 1, and
    // tile row = entity row - 5, and the two together give entity row - 4.
    //
    // Checked rather than argued. Level 1 read in play has its platforms on tile rows 8, 13 and 18,
    // and the port's own level 1 bitmap puts them on SolidMap rows 9, 14 and 19. The +1 is measured
    // on both sides. It also keeps EntityMover's constants lining up: that mover wraps a row at $1D,
    // which is map row 25, one past the map's twenty-five - exactly off the bottom.
    private const int RowBias = 4;

    // $0F51 and $0FE1. Above this the slot is doing something the movement must not touch, and the
    // routine returns before it probes anything.
    private const byte NoProbeType = 0x06;

    // $0F4C. Above this, $0F48 will not run its second step.
    private const byte NoRepeatType = 0x0C;

    // $0F94. What a slot that moved without being blocked is left as.
    private const byte MovedType = 0x04;

    // $104E. What a blocked slot with a negative AI state becomes.
    private const byte CaughtType = 0x34;

    // $0FD3. The rightmost column a thing may step into. There is no matching test on the left arm.
    private const byte LastColumn = 0x1C;

    // $0FAC and $0FDC. Eight pixels, which is one whole column.
    private const byte StepPixels = 0x08;

    // $1030 and $1040. The same sixteen pixel box the catch at $0D02 uses.
    private const byte CatchRange = 0x10;

    // $100E to $1014: subtract $14, snap to the eight pixel grid, add $13 back. The $13 is one less
    // than the $14 because the carry the subtraction left is added with it.
    private const byte GridOrigin = 0x14;
    private const byte GridMask = 0xF8;
    private const byte GridReturn = 0x13;

    // $ACB6 in game-tables-2.s, seven bytes. The AI state counter indexes it, so a counter walking
    // down from six gives $00 $02 $02 $02 $04 $04 $10 - which is exactly the order the VICE capture
    // of one bubble read its type byte in. The reference heads it a direction table; it is not one.
    //
    // **Plus the first two bytes of $ACBD, which is the next table along.** A bubble is blown with $88
    // in its counter, so its first two steps index eight and seven and the 6502 reads on past the
    // seven bytes into $ACBD's $00 $00. Stopping at seven throws on the first frame of every bubble.
    private static readonly byte[] s_types = [0x10, 0x04, 0x04, 0x02, 0x02, 0x02, 0x00, 0x00, 0x00];

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;
    private readonly BubbleCollision _collision;

    internal EnemyDispatcher(ObjectTable objects, EntityTable entities, BubbleCollision collision)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(collision);

        _objects = objects;
        _entities = entities;
        _collision = collision;
    }

    // $0F48. One step, and then a second one for a slot that qualifies.
    //
    // **The value the step indexes with is the state byte as it was before the step decremented
    // it.** $0F61's `and #$7F` masks whatever the accumulator already held, and `dec` is a memory
    // instruction that does not touch the accumulator. On the first call that value came from
    // $0CF8, before any decrement. On the second it came from $0F55, after the first. Reading
    // $0F61 on its own gives a routine that indexes with the decremented value both times, and
    // every animation in the game is then one frame ahead of the machine.
    internal void Step(int slot, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        Advance(slot, _objects.State[slot], map);

        if (_objects.Type[slot] >= NoRepeatType)
        {
            return;
        }

        // $0F53's `lda D_AA42,x` then `bpl`. Only a slot whose enemy type byte is negative steps
        // twice, and only while it still has a state left to spend.
        if ((_objects.EnemyType[slot] & 0x80) == 0 || _objects.State[slot] == 0)
        {
            return;
        }

        Advance(slot, _objects.State[slot], map);
    }

    // $0F61 and $0F91. Spend one of the state counter, turn what is left into a table index, and
    // move.
    private void Advance(int slot, byte counter, SolidMap map)
    {
        _objects.State[slot]--;

        int index = counter & 0x7F;

        // $0F66. A slot whose $AA30 is negative halves its index, and a half that reaches zero is
        // held at one rather than allowed to be zero.
        if ((_objects.Variant[slot] & 0x80) != 0)
        {
            index >>= 1;

            if (index == 0)
            {
                index = 1;
            }
        }

        // $0F91. Nothing here bounds the index - the 6502 reads whatever sits after the table. The
        // nine bytes above are as far as a counter is known to reach; past them this throws, so a
        // counter nobody has seen fails where it is made rather than somewhere downstream.
        _objects.Type[slot] = s_types[index];

        Move(slot, MovedType, map);
    }

    // $0F98. Eight pixels left or right, and three probes to say whether it was allowed.
    //
    // The two arms are not mirrors, and three differences would be smoothed away by writing them as
    // one. The right arm refuses to leave column $1C and the left arm has no bound at all. The left
    // arm probes $00, $29 and $50; the right probes $01, $28 and $51, which is not the same set
    // reflected. And the move happens *before* the probes, so a blocked thing has already been
    // moved and $100B is what puts it back.
    private void Move(int slot, byte restore, SolidMap map)
    {
        byte savedColumn = _objects.Column[slot];
        byte savedX = _objects.X[slot];

        if ((_objects.Direction[slot] & 0x80) != 0)
        {
            _objects.Column[slot]--;
            _objects.X[slot] -= StepPixels;

            // $0FB1's `jsr L105B`. A capture puts $18 or more in the type byte, which the test
            // below then reads as a reason to stop - so the two lines are one thing, not two.
            _ = _collision.Check(slot);

            if (_objects.Type[slot] >= NoProbeType)
            {
                return;
            }

            if (Struck(slot, map, Cell, BelowRight, TwoBelow))
            {
                Blocked(slot, restore, savedColumn, savedX);
            }

            return;
        }

        // $0FD1. The only bound either arm has.
        if (savedColumn == LastColumn)
        {
            Blocked(slot, restore, savedColumn, savedX);
            return;
        }

        _objects.Column[slot]++;
        _objects.X[slot] += StepPixels;

        // $0FE0's `jsr L105B`, as above.
        _ = _collision.Check(slot);

        if (_objects.Type[slot] >= NoProbeType)
        {
            return;
        }

        if (Struck(slot, map, CellRight, Below, TwoBelowRight))
        {
            Blocked(slot, restore, savedColumn, savedX);
        }
    }

    // $0FB8 to $0FFE. Three probes, and the first of them is skipped when the thing sits exactly on
    // a cell boundary.
    //
    // What decides that is $7BFE's return value, which is $A9D6 - the vertical sub-position. The
    // reference calls it a direction, and that is one more comment in this file that points the
    // wrong way: EntityMover drives the same byte as a sub-position, and a VICE capture of one
    // bubble's rise proves it counts eight and wraps.
    private bool Struck(int slot, SolidMap map, int first, int second, int third)
    {
        if (_objects.SubY[slot] != 0 && Solid(slot, map, first))
        {
            return true;
        }

        if (Solid(slot, map, second) || Solid(slot, map, third))
        {
            return true;
        }

        // $1001. A clean move clears the top bit of the state counter and leaves everything else.
        _objects.State[slot] &= 0x7F;
        return false;
    }

    private bool Solid(int slot, SolidMap map, int offset)
        => map[_objects.Row[slot] - RowBias + (offset / Stride), _objects.Column[slot] + (offset % Stride)];

    // $100B. Put the thing back where it was, and then decide what it now is.
    private void Blocked(int slot, byte restore, byte savedColumn, byte savedX)
    {
        // $100E. The `adc #$13` runs with the carry the subtraction left, so for any X at or above
        // $14 it adds twenty and not nineteen. Reading the operand alone puts every blocked thing
        // one pixel short of its cell.
        int difference = savedX - GridOrigin;
        _objects.X[slot] = (byte)(((byte)difference & GridMask) + GridReturn + (difference >= 0 ? 1 : 0));

        _objects.Column[slot] = savedColumn;

        byte type = restore;

        if ((_objects.State[slot] & 0x80) != 0)
        {
            // $101C. Player two and only player two - $BB and $C3 are the second byte of each of
            // those two arrays, and there is no indexing here at all. A blocked thing with a
            // negative state is caught whether or not anybody is near it; what the proximity
            // decides is only the byte written to $0193.
            _objects.Direction[slot] = Distance.Absolute(_entities.X[1], _objects.X[slot]) < CatchRange
                && Distance.Absolute(_entities.Y[1], _objects.Y[slot]) < CatchRange
                ? (byte)1
                : (byte)0;
            _objects.EnemyType[slot] = restore;
            type = CaughtType;
        }

        _objects.Type[slot] = type;

        // $1052. Both the horizontal sub-position and the state counter go to zero.
        _objects.SubX[slot] = 0;
        _objects.State[slot] = 0;
    }
}

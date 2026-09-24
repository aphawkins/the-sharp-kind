// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $EBC4 to $EC87 in entity-system.s: a walking enemy's jump, from the wind-up to the landing.
//
// Two jumps share this code, and the drift flags tell them apart.
//
//   * **Up**, onto the platform above. $EBC4 clears both drift flags and sets the rise counter to
//     $1F. The first sixteen frames are a wind-up: the thing stays where it is and turns to face the
//     other way every fourth frame. Then it rises by $ACDD for fifteen frames and falls by $ACDD.
//   * **Across** a gap in the floor. $EAD1 sets one drift flag and the rise counter to $0F, so there
//     is no wind-up. The thing rises one pixel a frame and moves two pixels across, then falls by
//     $ACED, which is one pixel a frame.
//
// The rise counter counts down to zero and then the fall counter counts up to $10. The fall ends
// on a floor, or after sixteen frames whatever is under it. Both counters go back to $FF then, and
// the walker's own checks find out if there is really a floor.
internal sealed class EnemyJump
{
    // $EBC4 and $EBDB. A rise counter at or above this is still winding up.
    private const byte WindUp = 0x1F;
    private const byte RiseStart = 0x10;

    // $EC17. Sixteen frames of fall, and the jump is over.
    private const byte FallEnd = 0x10;

    // $EC3C. Above this there is no floor to land on.
    private const byte LandCheckFloor = 0x1F;

    // $EC3E and $EC70. The row boundaries a landing is looked for on, and the one it snaps to.
    private const byte TopEdge = 0x15;
    private const byte GridBase = 0x2D;

    // $ECB6. The right-hand limit of the playfield. There is no left-hand one.
    private const byte RightEdge = 0xF4;

    // $ECAB and $ECD8. Two pixels across, by two `dec` or two `inc`.
    private const byte AcrossStep = 0x02;

    // $EC4A and $EC5C: the row the thing is in, which must be clear, and the row below, which must
    // be solid. The same pair as the descent at $EB48.
    private const int ClearRowLeft = 0x51;
    private const int ClearRowMiddle = 0x52;
    private const int ClearRowRight = 0x53;
    private const int FloorRowLeft = 0x79;
    private const int FloorRowMiddle = 0x7A;
    private const int FloorRowRight = 0x7B;

    // $EC8B and $ECBC. The cell beside the thing's middle row, or its bottom row when it is part
    // way down a row.
    private const int LeftMiddle = 0x28;
    private const int LeftBottom = 0x50;
    private const int RightMiddle = 0x2B;
    private const int RightBottom = 0x53;

    // $ACDD. The rise and the fall of the jump up.
    private static readonly byte[] s_jump =
    [
        0x00, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x03,
        0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x04, 0x04,
    ];

    // $ACED. The fall of the leap across.
    private static readonly byte[] s_leap =
    [
        0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
    ];

    private readonly EntityTable _entities;

    internal EnemyJump(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities;
    }

    // $EBC4. A jump up starts here, and $EBD9 runs its first frame at once.
    internal void StartUp(int slot, in PlayerCell cell, SolidMap map)
    {
        _entities.LeftFlag[slot] = 0;
        _entities.RightFlag[slot] = 0;
        _entities.Heading[slot] &= 0xFB;
        _entities.RiseCounter[slot] = WindUp;

        Step(slot, cell, map);
    }

    // $EBD9. The rise counter arrives in the accumulator, so every test below is on the value from
    // before this frame's `dec`.
    internal void Step(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte rise = _entities.RiseCounter[slot];

        if (rise == 0)
        {
            Fall(slot, cell, map);
            return;
        }

        _entities.RiseCounter[slot]--;

        // $EBFB. The wind-up. The count is at $1F to $10 here, so its low two bits are clear every
        // fourth frame.
        if (rise >= RiseStart)
        {
            if ((rise & 0x03) == 0)
            {
                _entities.Frame[slot] ^= _entities.FrameCount[slot];
            }

            return;
        }

        // $EBE7. A leap rises one pixel and moves across. A jump up rises by the table.
        if (Leaping(slot))
        {
            _entities.Y[slot]--;
            Across(slot, cell, map);
            return;
        }

        _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] - s_jump[rise]));
        EntityAnimation.Step(_entities, slot);
    }

    // $EC94 and $ECC3. Only a wall with open space behind it stops the thing. A solid cell with
    // another solid cell behind it lets the thing move on, so a thing inside a wall is not held.
    private static bool Moves(in PlayerCell cell, SolidMap map, int beside, int behind)
        => cell.FineX != 0 || !cell.Solid(map, beside) || cell.Solid(map, behind);

    // Three cells of a row, the third only when the thing straddles a column.
    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left) || cell.Solid(map, middle) || (cell.FineX != 0 && cell.Solid(map, right));

    // $EC0C.
    private void Fall(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.FallCounter[slot] & 0x80) != 0)
        {
            _entities.FallCounter[slot] = 0;
        }

        byte fall = _entities.FallCounter[slot];

        if (fall == FallEnd)
        {
            End(slot);
            return;
        }

        _entities.FallCounter[slot]++;

        byte y = unchecked((byte)(_entities.Y[slot] + (Leaping(slot) ? s_leap[fall] : s_jump[fall])));
        _entities.Y[slot] = y;

        // $EC3C. A floor is only looked for as the thing crosses into a new row: the fine row it has
        // reached is smaller than the one it started the frame on.
        if (y < LandCheckFloor || (unchecked((byte)(y - TopEdge)) & 0x07) >= cell.FineY)
        {
            Across(slot, cell, map);
            return;
        }

        // The probes are from the cell the frame started on, not the one the fall reached.
        if (Blocked(cell, map, ClearRowLeft, ClearRowMiddle, ClearRowRight)
            || !Blocked(cell, map, FloorRowLeft, FloorRowMiddle, FloorRowRight))
        {
            Across(slot, cell, map);
            return;
        }

        // $EC70.
        _entities.Y[slot] = unchecked((byte)(((y - GridBase) & 0xF8) + GridBase));
        End(slot);
    }

    // $EC7C.
    private void End(int slot)
    {
        _entities.RiseCounter[slot] = 0xFF;
        _entities.FallCounter[slot] = 0xFF;
        EntityAnimation.Step(_entities, slot);
    }

    // $EC87. A drift flag that is not negative is off. $EAD1 sets one to $FF, and a wall sets it to
    // $01, so the leap stops moving across and goes on falling.
    private void Across(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.LeftFlag[slot] & 0x80) != 0)
        {
            int beside = cell.FineY == 0 ? LeftMiddle : LeftBottom;

            if (Moves(cell, map, beside, beside + 1))
            {
                _entities.X[slot] = unchecked((byte)(_entities.X[slot] - AcrossStep));
            }
            else
            {
                _entities.LeftFlag[slot] = 0x01;
            }
        }
        else if ((_entities.RightFlag[slot] & 0x80) != 0)
        {
            int beside = cell.FineY == 0 ? RightMiddle : RightBottom;

            if (_entities.X[slot] != RightEdge && Moves(cell, map, beside, beside - 1))
            {
                _entities.X[slot] = unchecked((byte)(_entities.X[slot] + AcrossStep));
            }
            else
            {
                _entities.RightFlag[slot] = 0x01;
            }
        }

        EntityAnimation.Step(_entities, slot);
    }

    // $EBE7 and $EC23. Either drift flag not zero, whatever its sign.
    private bool Leaping(int slot) => (_entities.LeftFlag[slot] | _entities.RightFlag[slot]) != 0;
}

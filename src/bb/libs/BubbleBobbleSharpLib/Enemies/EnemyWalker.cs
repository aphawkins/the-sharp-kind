// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $EA08 in entity-system.s, with $EE62, $EAFA and the two side movers: an enemy that walks the
// platforms, falls off their ends, and jumps to reach its player.
//
// This is state 2 of the $1E3A table, so class 0, which is level 1's enemy. States 3, 7 and 8 reach
// it too, through $E9FD.
//
// **Which way the thing faces is its frame.** A frame below FrameCount faces right and one at or above
// it faces left, and every decision here reads the frame as it was when $1E6C copied it into $25. A
// turn earlier in the same frame does not change what a later test sees.
//
// **The player the thing follows is the caller's.** $1CF3 stores it in $4B before $1E6C runs.
//
// **The leap reads past the level.** $EA92 looks nine columns along the row two below the thing. Near
// either wall that column is off the level's thirty-two, and the 6502 reads one of the eight bytes of
// entity arrays that end each forty-byte row of $8500 - SolidMap's row tails. A capture on level 50
// proved it: a class 5 enemy at column 25 did not leap, because the byte nine columns on was slot 2's
// $8840, and that is zero.
internal sealed class EnemyWalker
{
    // $85E8. Bit 0 walks left, bit 1 walks right and bit 2 jumps up.
    private const byte LeftBit = 0x01;
    private const byte RightBit = 0x02;
    private const byte UpBit = 0x04;

    // $EA6D. Above this the thing is off the top of the level and falls without looking.
    private const byte GroundCheckFloor = 0x1F;

    // $EAAA. A leap to the right starts only here, six pixels into a column.
    private const int LeapRightFineX = 0x06;

    // $EAD1 and $EA92. The leap needs an open cell two rows down, beside the thing's feet, and a
    // solid one nine columns further on. The far one is read through a pointer nine bytes along, so
    // its offset is $50 plus or minus nine.
    private const int LeapLeftGap = 0x50;
    private const int LeapRightGap = 0x53;
    private const int LeapLeftFar = 0x50 - 0x09;
    private const int LeapRightFar = 0x50 + 0x09;

    // $EAE1 and $EAF4. A leap starts part way up, with no wind-up.
    private const byte LeapRise = 0x0F;

    // $EA78. The three cells under the thing.
    private const int FloorLeft = 0x51;
    private const int FloorMiddle = 0x52;
    private const int FloorRight = 0x53;

    // $ECED and $ED2B. The cell beside the thing's middle row, or its bottom row when it is part way
    // down a row.
    private const int LeftMiddle = 0x28;
    private const int LeftBottom = 0x50;
    private const int RightMiddle = 0x2B;
    private const int RightBottom = 0x53;

    // $ECF5 and $ED33. Two pixels a step, as an add.
    private const byte StepLeft = 0xFE;
    private const byte StepRight = 0x02;

    // $EA9C. Class 1 does not leap.
    private const byte NoLeapState = 0x03;

    private readonly EntityTable _entities;
    private readonly EnemyChase _chase;
    private readonly EnemyJump _jump;
    private readonly PlayerDescent _descent;

    internal EnemyWalker(EntityTable entities, EnemyChase chase, EnemyJump jump, PlayerDescent descent)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(chase);
        ArgumentNullException.ThrowIfNull(jump);
        ArgumentNullException.ThrowIfNull(descent);

        _entities = entities;
        _chase = chase;
        _jump = jump;
        _descent = descent;
    }

    // $EA08. The cell is the one $1E6C found, and target is the slot $1CF3 put in $4B.
    internal void Step(int slot, int target, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        // $1E6F. The frame as the thing starts the frame, and the only one the tests below read.
        byte facing = _entities.Frame[slot];

        _chase.Step(slot, target);

        // $EA0E. Copied after $EE62 has had its say, and read a bit at a time by $EAFA.
        byte heading = _entities.Heading[slot];

        // $EA13. Not on the ground, and not jumping, so falling.
        if ((_entities.GroundState[slot] & 0x80) == 0)
        {
            Descend(slot, cell, map);
            return;
        }

        if ((_entities.RiseCounter[slot] & 0x80) == 0)
        {
            _jump.Step(slot, cell, map);
            return;
        }

        // $EA20. Asked whether it is wanted or not, as long as it is allowed.
        if (_entities.ClimbFlag[slot] != 0 && EnemyProbes.PlatformAbove(cell, map))
        {
            _entities.Heading[slot] |= UpBit;
            heading = _entities.Heading[slot];
        }

        if (_entities.Y[slot] < GroundCheckFloor)
        {
            StartFall(slot, cell, map);
            return;
        }

        // $EA73. Part way down a row the ground is not looked at.
        if (cell.FineY != 0)
        {
            Walk(slot, facing, heading, cell, map);
            return;
        }

        if (!EnemyProbes.Blocked(cell, map, FloorLeft, FloorMiddle, FloorRight))
        {
            StartFall(slot, cell, map);
            return;
        }

        if (!Leap(slot, facing, cell, map))
        {
            Walk(slot, facing, heading, cell, map);
        }
    }

    // $EA92 to $EAF2. On a row boundary, with ground under it: a leap across a gap ahead, if the
    // thing is allowed one and there is somewhere to land. False leaves the frame to the walk.
    private bool Leap(int slot, byte facing, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.LeapFlag[slot] | _entities.ClimbFlag[slot]) == 0 || _entities.State[slot] == NoLeapState)
        {
            return false;
        }

        bool left = facing >= _entities.FrameCount[slot];

        // $EAB0 and $EAC4. The left leap goes from a column boundary. The right one goes from six
        // pixels in, and the `adc #$08` after the `cmp` that found six runs with the carry set.
        if (cell.FineX != (left ? 0 : LeapRightFineX))
        {
            return false;
        }

        if (cell.Solid(map, left ? LeapLeftGap : LeapRightGap) || !cell.Solid(map, left ? LeapLeftFar : LeapRightFar))
        {
            return false;
        }

        _entities.LeftFlag[slot] = left ? (byte)0xFF : (byte)0x00;
        _entities.RightFlag[slot] = left ? (byte)0x00 : (byte)0xFF;
        _entities.RiseCounter[slot] = LeapRise;

        _jump.Step(slot, cell, map);
        return true;
    }

    // $EAFA. The heading as it was copied, a bit at a time. The turn one bit makes does not stop the
    // next bit being read as it was.
    private void Walk(int slot, byte facing, byte heading, in PlayerCell cell, SolidMap map)
    {
        if ((heading & LeftBit) != 0)
        {
            WalkLeft(slot, facing, cell, map);
        }

        if ((heading & RightBit) != 0)
        {
            WalkRight(slot, facing, cell, map);
        }

        if ((heading & UpBit) != 0)
        {
            _jump.StartUp(slot, cell, map);
            return;
        }

        EntityAnimation.Step(_entities, slot);
    }

    // $ECDF. A thing facing right turns to face left, and moves on the next frame.
    private void WalkLeft(int slot, byte facing, in PlayerCell cell, SolidMap map)
    {
        if (facing < _entities.FrameCount[slot])
        {
            _entities.Frame[slot] = _entities.FrameCount[slot];
            return;
        }

        EnemyProbes.Sidestep(_entities, slot, cell, map, cell.FineY == 0 ? LeftMiddle : LeftBottom, StepLeft);
    }

    // $ED1E.
    private void WalkRight(int slot, byte facing, in PlayerCell cell, SolidMap map)
    {
        if (facing >= _entities.FrameCount[slot])
        {
            _entities.Frame[slot] = 0;
            return;
        }

        EnemyProbes.Sidestep(_entities, slot, cell, map, cell.FineY == 0 ? RightMiddle : RightBottom, StepRight);
    }

    // $EA8C. Out of $FF and into a fall, on the same frame.
    private void StartFall(int slot, in PlayerCell cell, SolidMap map)
    {
        _entities.GroundState[slot]++;
        Descend(slot, cell, map);
    }

    // $EB3F. The descent, then $EB0F whichever way it ends.
    private void Descend(int slot, in PlayerCell cell, SolidMap map)
    {
        _descent.Step(slot, cell, map);
        EntityAnimation.Step(_entities, slot);
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $1F2E to $214A in player-animation.s: state 4 of the $1E3A table, an enemy that only ever moves by
// hopping. The spawn gives class 2 this state.
//
// The reference files it with the players' animation, and heads it as a player's jump. It is not: it
// runs for enemy slots only, through $1E3A.
//
// It has the walker's two jumps, and it takes one on every frame it stands on the ground.
//
//   * **Up**, onto a platform above, when $EE62 allows a climb and there is one. The rise counter
//     starts at $1F and the first sixteen frames are a wind-up that turns it round every fourth.
//   * **Across** otherwise. It may turn to face its player first, on one draw in eight and a half,
//     when $EE62 allows a leap. Then it sets off the way it faces, with the rise counter at $0F.
//
// **The spring is the frame's low two bits.** A hop does not rise until those bits are below 2, and
// each frame before that takes one off. The fall stops as soon as they are 2 or more, and the landing
// counts them up to 3 and then ends the hop. So the sprite crouches, rises, and crouches again.
//
// **A falling hopper animates only as it lands.** $EBB8 sets up the descent at $EB48 with an `rts`
// for $EBB5's continuation, which every exit but the landing takes. The landing leaves through $EB94
// instead, and $EBB8 writes only that instruction's opcode - a `jmp` - and not its operand, which is
// $EB0F. A capture on level 36 showed it: every landing stepped the frame.
internal sealed class EnemyHopper
{
    // $1F43. Above this the thing is off the top of the level and falls without looking.
    private const byte GroundCheckFloor = 0x1F;

    // $1F47 and $207C. The three cells under the thing.
    private const int FloorLeft = 0x51;
    private const int FloorMiddle = 0x52;
    private const int FloorRight = 0x53;

    // $2090. The row below those, which a landing must find solid.
    private const int BelowLeft = 0x79;
    private const int BelowMiddle = 0x7A;
    private const int BelowRight = 0x7B;

    // $1FAB and $1FEA. The two jumps' rise counters.
    private const byte UpRise = 0x1F;
    private const byte AcrossRise = 0x0F;

    // $1FF1. At or above this, the rise counter is winding up.
    private const byte RiseStart = 0x10;

    // $2042. Sixteen frames of fall at most.
    private const byte FallEnd = 0x10;

    // $1FBB. Turn to face the player on a draw below this.
    private const byte TurnChance = 0x1E;

    // $1FC8, $1FCC, $202D and $2031. The frames a turn lands on: the last of each block of four.
    private const byte FaceLeft = 0x07;
    private const byte FaceRight = 0x03;

    // $1FFB and $204E. The spring is the frame's low two bits.
    private const byte SpringMask = 0x03;
    private const byte Crouched = 0x02;

    // $2070, $2074 and $20A7. The landing's row checks, as the walker's jump has them.
    private const byte LandCheckFloor = 0x1F;
    private const byte TopEdge = 0x15;
    private const byte GridBase = 0x2D;

    // $20DE and $2115. Above this the walls are not looked at.
    private const byte WallCheckFloor = 0x2D;

    // $20E6 to $20F6, and $211D to $212D. The three cells beside the thing, the third only when it is
    // part way down a row.
    private const int LeftTop = 0x00;
    private const int LeftMiddle = 0x28;
    private const int LeftBottom = 0x50;
    private const int RightTop = 0x03;
    private const int RightMiddle = 0x2B;
    private const int RightBottom = 0x53;

    // $2107 and $2146. Two pixels across.
    private const byte AcrossStep = 0x02;

    // $213E. A wall flips the thing's facing.
    private const byte FacingBit = 0x04;

    // $ACDD and $ACED, the same two tables EnemyJump reads.
    private static readonly byte[] s_jump =
    [
        0x00, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x03,
        0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x04, 0x04,
    ];

    private static readonly byte[] s_leap =
    [
        0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
    ];

    private readonly EntityTable _entities;
    private readonly EnemyChase _chase;
    private readonly PlayerDescent _descent;
    private readonly BbRandom _random;

    internal EnemyHopper(EntityTable entities, EnemyChase chase, PlayerDescent descent, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(chase);
        ArgumentNullException.ThrowIfNull(descent);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _chase = chase;
        _descent = descent;
        _random = random;
    }

    // $1F2E. The cell is the one $1E6C found, and target is the slot $1CF3 put in $4B.
    internal void Step(int slot, int target, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        // $1E6F's copy of the frame, which $1FDB reads unless $1FCE has replaced it.
        byte facing = _entities.Frame[slot];

        _chase.Step(slot, target);

        // $1F31. Falling.
        if ((_entities.GroundState[slot] & 0x80) == 0)
        {
            Descend(slot, cell, map);
            return;
        }

        if ((_entities.RiseCounter[slot] & 0x80) == 0)
        {
            Hop(slot, _entities.RiseCounter[slot], cell, map);
            return;
        }

        // $1F41 and $1F5B. No ground, so a fall starts.
        if (_entities.Y[slot] < GroundCheckFloor || !EnemyProbes.Blocked(cell, map, FloorLeft, FloorMiddle, FloorRight))
        {
            _entities.GroundState[slot]++;
            Descend(slot, cell, map);
            return;
        }

        // $1F61 to $1FB0. A climb, if one is allowed and there is somewhere to climb to.
        if (_entities.ClimbFlag[slot] != 0 && EnemyProbes.PlatformAbove(cell, map))
        {
            _entities.LeftFlag[slot] = 0;
            _entities.RightFlag[slot] = 0;
            _entities.RiseCounter[slot] = UpRise;
            Hop(slot, UpRise, cell, map);
            return;
        }

        // $1FB3. A turn to face the player, if a leap is allowed and the draw falls right.
        if (_entities.LeapFlag[slot] != 0 && _random.Next() < TurnChance)
        {
            facing = _entities.X[target] >= _entities.X[slot] ? FaceRight : FaceLeft;
            _entities.Frame[slot] = facing;
        }

        // $1FD3. Off the way it faces.
        bool left = facing >= _entities.FrameCount[slot];
        _entities.LeftFlag[slot] = left ? (byte)0xFF : (byte)0x00;
        _entities.RightFlag[slot] = left ? (byte)0x00 : (byte)0xFF;
        _entities.RiseCounter[slot] = AcrossRise;
        Hop(slot, AcrossRise, cell, map);
    }

    // $1FEF. The rise counter arrives in the accumulator, so every test is on the value from before
    // this frame's `dec`.
    private void Hop(int slot, byte rise, in PlayerCell cell, SolidMap map)
    {
        if (rise == 0)
        {
            Fall(slot, cell, map);
            return;
        }

        // $201E. The wind-up turns the thing round on every fourth frame.
        if (rise >= RiseStart)
        {
            _entities.RiseCounter[slot]--;

            if ((rise & 0x03) == 0)
            {
                _entities.Frame[slot] = _entities.Frame[slot] >= _entities.FrameCount[slot] ? FaceRight : FaceLeft;
            }

            return;
        }

        // $1FF6. Still crouched: one off the spring, and nothing else.
        if ((_entities.Frame[slot] & SpringMask) >= Crouched)
        {
            _entities.Frame[slot]--;
            return;
        }

        _entities.RiseCounter[slot]--;

        // $2006. The leap rises a pixel and moves across. The jump up rises by the table.
        if (Leaping(slot))
        {
            _entities.Y[slot]--;
            Across(slot, cell, map);
            return;
        }

        _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] - s_jump[rise]));
        EntityAnimation.Toggle(_entities, slot);
    }

    // $2037.
    private void Fall(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.FallCounter[slot] & 0x80) != 0)
        {
            _entities.FallCounter[slot] = 0;
        }

        byte fall = _entities.FallCounter[slot];

        if (fall == FallEnd || (_entities.Frame[slot] & SpringMask) >= Crouched)
        {
            Land(slot);
            return;
        }

        _entities.FallCounter[slot]++;

        byte y = unchecked((byte)(_entities.Y[slot] + (Leaping(slot) ? s_leap[fall] : s_jump[fall])));
        _entities.Y[slot] = y;

        // $2070. A floor is only looked for as the thing crosses into a new row, from the cell the
        // frame started on.
        if (y < LandCheckFloor
            || (unchecked((byte)(y - TopEdge)) & 0x07) >= cell.FineY
            || EnemyProbes.Blocked(cell, map, FloorLeft, FloorMiddle, FloorRight)
            || !EnemyProbes.Blocked(cell, map, BelowLeft, BelowMiddle, BelowRight))
        {
            Across(slot, cell, map);
            return;
        }

        // $20A4.
        _entities.Y[slot] = unchecked((byte)(((y - GridBase) & 0xF8) + GridBase));
        Land(slot);
    }

    // $20B0. The spring counts back up to 3, and then the hop is over. A frame of 0 or 1 is set to 2
    // in its own block, which starts the count.
    private void Land(int slot)
    {
        int spring = _entities.Frame[slot] & SpringMask;

        if (spring == SpringMask)
        {
            _entities.RiseCounter[slot] = 0xFF;
            _entities.FallCounter[slot] = 0xFF;
            EntityAnimation.Toggle(_entities, slot);
            return;
        }

        _entities.Frame[slot] = spring >= Crouched
            ? (byte)(_entities.Frame[slot] + 1)
            : (byte)((_entities.Frame[slot] & FacingBit) | Crouched);
    }

    // $20D7. A wall turns the drift round and flips the facing. Near the top of the level the walls
    // are not looked at, and there is no right-hand limit, unlike the walker's.
    private void Across(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.LeftFlag[slot] & 0x80) != 0)
        {
            if (Walled(slot, cell, map, LeftTop, LeftMiddle, LeftBottom))
            {
                _entities.LeftFlag[slot] = 0;
                _entities.RightFlag[slot]--;
                _entities.Frame[slot] ^= FacingBit;
            }
            else
            {
                _entities.X[slot] = unchecked((byte)(_entities.X[slot] - AcrossStep));
            }
        }
        else if ((_entities.RightFlag[slot] & 0x80) != 0)
        {
            if (Walled(slot, cell, map, RightTop, RightMiddle, RightBottom))
            {
                _entities.RightFlag[slot] = 0;
                _entities.LeftFlag[slot]--;
                _entities.Frame[slot] ^= FacingBit;
            }
            else
            {
                _entities.X[slot] = unchecked((byte)(_entities.X[slot] + AcrossStep));
            }
        }

        EntityAnimation.Toggle(_entities, slot);
    }

    // $20DC to $20FA. The third cell counts only when the thing is part way down a row.
    private bool Walled(int slot, in PlayerCell cell, SolidMap map, int top, int middle, int bottom)
        => _entities.Y[slot] >= WallCheckFloor
            && cell.FineX == 0
            && (cell.Solid(map, top) || cell.Solid(map, middle) || (cell.FineY != 0 && cell.Solid(map, bottom)));

    // $EBB8. The descent, and $EB0F only when it lands.
    private void Descend(int slot, in PlayerCell cell, SolidMap map)
    {
        byte ground = _entities.GroundState[slot];

        _descent.Step(slot, cell, map);

        if (_entities.GroundState[slot] != ground)
        {
            EntityAnimation.Step(_entities, slot);
        }
    }

    // $2006 and $2055. Either drift flag not zero, whatever its sign.
    private bool Leaping(int slot) => (_entities.LeftFlag[slot] | _entities.RightFlag[slot]) != 0;
}

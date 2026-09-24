// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Items;

// What becomes of an enemy once its bubble is popped: it flies, it falls, it lands as food, and a
// player picks the food up or it goes.
//
// The enemy stays in its own slot of the eight the whole way, and each step is a state of the $1E3A
// table, so EnemyFrame dispatches to it like any enemy class:
//
//   * $1A6F, **Kill**, starts it. The state goes to $0B and the flight is rolled at random.
//   * State $0B, $267B and $26BF, is the flight: across the level and bouncing off the side walls,
//     up and then down, until the fall it rolled has run out.
//   * State $0C, $273D, is the fall: two pixels a frame until there is a platform below.
//   * State $11, $2921, turns the enemy into food. Only one slot a frame may, because it copies the
//     food's picture into the slot's sprite, and $1CBD reopens the gate each frame.
//   * State $12, $29BB, waits $C8 frames for a player to touch it, and then scores and goes.
//
// AttackTimer, $8890, says which food, and so which score: Kill sets it to 9 and the pop that calls
// Kill puts its chain count there after. LeftFlag, $8840, keeps the class the enemy had, because the
// flight's frames are offset by it.
//
// **Not here:** the pop itself, which is $3D2D and $3D77 in the sprite renderer's dispatch; the
// picture $2921 copies, which is rendering and follows from AttackTimer; and $29FF's store of 1 in
// $B0, which nothing translated reads.
internal sealed class FoodDrop
{
    // The states of the $1E3A table this class is.
    internal const byte FlyingState = 0x0B;
    internal const byte FallingState = 0x0C;
    internal const byte LandedState = 0x11;
    internal const byte FoodState = 0x12;

    // $1A71 and $1A7A. An enemy in state $0A keeps its class in RiseCounter; $0D keeps none.
    private const byte RisingState = 0x0A;
    private const byte NoClassState = 0x0D;

    // $1AA4, $1AAA and $1AAF.
    private const byte KillBubbleTimer = 0x07;
    private const byte KillFood = 0x09;
    private const byte KillColour = 0x0E;

    // $2681. The flight turns or ends on one frame in four.
    private const byte FlightPace = 0x03;

    // $26CC, $26D0, $26E1 and $26E5. The side walls.
    private const byte RightWall = 0xF4;
    private const byte LeftWall = 0x24;

    // $26F9, $26FD, $270B and $270F. The top and the bottom of the playfield, which wrap into each other.
    private const byte Top = 0x15;
    private const byte Bottom = 0xF5;

    // $2740. The fall.
    private const byte FallStep = 0x02;

    // $274C and $2750. The fall only looks for a platform on rows that line up with one.
    private const byte FirstRow = 0x1F;
    private const byte RowOrigin = 0x2D;

    // $2756 to $277C. Two rows below the thing is inside the platform, and three rows below is under it.
    private const int InsideLeft = 0x51;
    private const int UnderLeft = 0x79;

    // $2931 and $293A.
    private const byte FoodLife = 0xC8;

    // $2946. (sprite_data_7C40 + $40 - $4000) / 64: the block of sprites the six food slots draw from.
    private const byte FoodSpriteBase = 0xF2;

    // $29DB and $29EB.
    private const byte Reach = 0x10;

    // $29C7 and $29CB. A player dying still picks food up; a dead one does not.
    private const byte DeadState = 0x0F;

    // $29FF. The one score that goes in the lowest byte rather than the one above it.
    private const byte LowScore = 0x50;

    // $AB89. The flight's frames, offset by the class the enemy had.
    private static readonly byte[] s_flightFrames = [0x07, 0x05, 0x07, 0x07, 0x07, 0x03, 0x07, 0x03, 0x03, 0x01];

    // $A790, indexed by AttackTimer: which food. Entry 0 is the high byte of sprite_data_7C40 + $200,
    // the byte before the table proper, and nothing sets AttackTimer to 0 on the way here.
    private static readonly byte[] s_foods = [0x7E, 0x08, 0x11, 0x02, 0x18, 0x0A, 0x1D, 0x29, 0x2B, 0x2C];

    // $A79A, indexed by AttackTimer: the score. The tenth byte is $A7A3, which the table runs into.
    private static readonly byte[] s_scores = [0x22, 0x50, 0x01, 0x02, 0x03, 0x04, 0x05, 0x08, 0x09, 0x0A];

    // $A8E4, indexed by the food. The low three bits are the sprite's colour.
    private static readonly byte[] s_colours =
    [
        0x0C, 0x09, 0x09, 0x09, 0x0F, 0x0A, 0x0A, 0x0E, 0x0F, 0x0F, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0F,
        0x0B, 0x0F, 0x0A, 0x0C, 0x0D, 0x0F, 0x0F, 0x0A, 0x0D, 0x09, 0x0A, 0x0A, 0x0F, 0x0F, 0x0F, 0x09,
        0x0F, 0x0A, 0x0C, 0x0B, 0x0F, 0x0A, 0x0F, 0x0F, 0x0A, 0x0F, 0x0D, 0x0D, 0x0C, 0x0F, 0x0F,
    ];

    private readonly EntityTable _entities;
    private readonly Scores _scores;
    private readonly BbRandom _random;

    // $2922, the operand of $2921's own `lda #`. Not zero once a slot has become food this frame.
    private bool _placed;

    internal FoodDrop(EntityTable entities, Scores scores, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _scores = scores;
        _random = random;
    }

    // $1CC7's `stx D_2922`, once a frame before the slots are walked.
    internal void BeginFrame() => _placed = false;

    // $1A6F. enemy is 0 to 5, the index $105B and $3D77 use, so the slot is two along.
    internal void Kill(int enemy)
    {
        int slot = enemy + 2;
        byte kind = _entities.State[slot];

        if (kind == RisingState)
        {
            kind = _entities.RiseCounter[slot];
        }

        if (kind != NoClassState)
        {
            _entities.LeftFlag[slot] = kind;
        }

        _entities.State[slot] = FlyingState;
        _entities.EnemyCount--;
        _entities.RiseCounter[slot] = (byte)((_random.Next() & 0x07) + 1);

        byte climb = (byte)((_random.Next() & 0x07) + 1);
        _entities.FallCounter[slot] = climb;
        _entities.RightFlag[slot] = climb;

        _entities.GroundState[slot] = (byte)(_random.Next() & 0x01);
        _entities.BubbleTimer[slot] = KillBubbleTimer;
        _entities.AttackTimer[slot] = KillFood;
        _entities.Colour[slot] = KillColour;
    }

    // $267B. RiseCounter is the sideways speed and GroundState which way. FallCounter is positive on
    // the way up and holds the rise's speed in RightFlag, which counts down to the turn; on the way
    // down it is the fall's length with bit 7 set, and RightFlag counts up to it.
    internal void Fly(int slot)
    {
        _entities.BubbleTimer[slot]--;

        if ((sbyte)_entities.BubbleTimer[slot] < 0)
        {
            _entities.BubbleTimer[slot] = FlightPace;

            if ((sbyte)_entities.FallCounter[slot] >= 0)
            {
                _entities.RightFlag[slot]--;

                if (_entities.RightFlag[slot] == 0)
                {
                    _entities.FallCounter[slot] = (byte)(((_random.Next() & 0x07) + 1) | 0x80);
                }
            }
            else
            {
                _entities.RightFlag[slot]++;

                // $26AA. `bcc` and `beq`: only a count past the length ends the flight.
                if ((_entities.RightFlag[slot] | 0x80) > _entities.FallCounter[slot])
                {
                    _entities.X[slot] &= 0xFE;
                    _entities.Y[slot] |= 0x01;
                    _entities.State[slot] = FallingState;
                    return;
                }
            }
        }

        Travel(slot);
    }

    // $273D, with the cell $1E6C found before the step. Every row that lines up with a platform, it
    // looks two rows down and then three: inside a platform it goes on through it, and above one it
    // has landed.
    internal void Fall(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte y = unchecked((byte)(_entities.Y[slot] + FallStep));

        if (y == Bottom)
        {
            y = Top;
        }

        _entities.Y[slot] = y;

        if (y >= FirstRow
            && ((y - RowOrigin) & 0x07) == 0
            && !EnemyProbes.Blocked(cell, map, InsideLeft, InsideLeft + 1, InsideLeft + 2)
            && EnemyProbes.Blocked(cell, map, UnderLeft, UnderLeft + 1, UnderLeft + 2))
        {
            _entities.State[slot] = LandedState;
        }

        Animate(slot);
    }

    // $2921. The frame is the slot, because each of the six has its own sprite to copy the food into.
    internal void Land(int slot)
    {
        if (_placed)
        {
            return;
        }

        _placed = true;

        byte food = s_foods[_entities.AttackTimer[slot]];

        _entities.State[slot]++;
        _entities.RiseCounter[slot] = FoodLife;
        _entities.Frame[slot] = (byte)slot;
        _entities.SpriteBase[slot] = FoodSpriteBase;
        _entities.Colour[slot] = (byte)(s_colours[food] & 0x07);
        _entities.FlashTimer[slot] = 0;
    }

    // $29BB. Player two first, and the first in reach takes it.
    internal void Wait(int slot)
    {
        _entities.RiseCounter[slot]--;

        if (_entities.RiseCounter[slot] == 0)
        {
            Clear(slot);
            return;
        }

        for (int player = 1; player >= 0; player--)
        {
            byte state = _entities.State[player];

            if (state is 0 or DeadState
                || Distance.Absolute(_entities.X[slot], _entities.X[player]) >= Reach
                || Distance.Absolute(_entities.Y[slot], _entities.Y[player]) >= Reach)
            {
                continue;
            }

            byte score = s_scores[_entities.AttackTimer[slot]];
            int index = Scores.Last(player);

            if (score != LowScore)
            {
                index--;
            }

            _scores.Add(index, score);
            Clear(slot);
            return;
        }
    }

    // $26BF. Sideways to a wall and back, then up or down, wrapping top to bottom.
    private void Travel(int slot)
    {
        byte speed = _entities.RiseCounter[slot];

        if (_entities.GroundState[slot] == 0)
        {
            _entities.X[slot] = unchecked((byte)(_entities.X[slot] + speed));

            if (_entities.X[slot] >= RightWall)
            {
                _entities.X[slot] = RightWall;
                _entities.GroundState[slot]++;
            }
        }
        else
        {
            _entities.X[slot] = unchecked((byte)(_entities.X[slot] - speed));

            if (_entities.X[slot] < LeftWall)
            {
                _entities.X[slot] = LeftWall;
                _entities.GroundState[slot]--;
            }
        }

        byte climb = _entities.RightFlag[slot];

        if ((sbyte)_entities.FallCounter[slot] >= 0)
        {
            _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] - climb));

            if (_entities.Y[slot] < Top)
            {
                _entities.Y[slot] = Bottom;
            }
        }
        else
        {
            _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] + climb));

            if (_entities.Y[slot] >= Bottom)
            {
                _entities.Y[slot] = Top;
            }
        }

        Animate(slot);
    }

    // $2713. Heading is the frame counter here, and a quarter of it picks one of four frames.
    private void Animate(int slot)
    {
        _entities.Heading[slot]++;

        int step = (_entities.Heading[slot] >> 2) & 0x03;

        if (step != 0)
        {
            step += s_flightFrames[_entities.LeftFlag[slot]];
        }

        _entities.Frame[slot] = (byte)step;
        _entities.FlashTimer[slot] = 0;
    }

    // $2A0B.
    private void Clear(int slot)
    {
        _entities.X[slot] = 0;
        _entities.Y[slot] = 0;
        _entities.State[slot] = 0;
    }
}

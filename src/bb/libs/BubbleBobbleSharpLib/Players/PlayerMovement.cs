// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

// $220C, $226B and $22C3 in entity-interaction.s: what a player does with the stick, left and right.
//
// Found with VICE rather than by reading, and the reading had it wrong. A store watchpoint on $BA
// with right held traps three hundred times at $22C2, which is the instruction after the sta FA,x at
// $22C0 - the last of the tail at L22BB that both movers fall into. The comments in the reference
// call these two "move left in bubble" and "move right in bubble". They are nothing of the kind:
// they are how a player walks. Section 1 settles it, the code is the golden source, and this is the
// third comment in that file to point the wrong way.
//
// The shape is worth keeping in mind while reading this class, because it is not one routine per
// direction. Each mover does three things - it writes its own delta into $04, picks its own probe
// offset, and decides from the current frame whether the player already faces that way - and then
// both fall into one piece of common code that checks a wall and adds the delta. So a player facing
// the other way turns on the first push and walks on the next.
internal sealed class PlayerMovement
{
    // $226B and $22C3 load these into $04. Two pixels, and the byte add at L22BB wraps like the
    // 6502's - see section 6.1.
    private const byte LeftStep = 0xFE;
    private const byte RightStep = 0x02;

    // $8520 values the movers write to face a player. The walk cycle then alternates bit 0, so
    // facing left is really the pair 4 and 5, and facing right the pair 0 and 1.
    private const byte FaceLeft = 0x04;
    private const byte FaceRight = 0x00;

    // The cells each mover looks at, in the offsets $226B and $22C3 name. Which of a pair is used
    // depends on whether the player sits on a row boundary: $24 is the fine Y offset, and a player
    // part way down a row has to look a row further on.
    private const int LeftProbeAligned = 0x28;
    private const int LeftProbeSplit = 0x50;
    private const int RightProbeAligned = 0x2B;
    private const int RightProbeSplit = 0x53;

    // $22A6. Above this row a player is off the top of the level, where there is no wall to walk
    // into and the check is skipped.
    private const byte WallCheckFloor = 0x2D;

    // $2294. The walk steps on every fourth frame.
    private const byte AnimationPeriod = 0x04;

    // $222B. What a jump's rise counter starts at, and what a latched drift flag holds.
    private const byte JumpRise = 0x0F;
    private const byte Latched = 0xFF;

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;
    private readonly BubbleBlow _blow;

    internal PlayerMovement(PlayerTable players, EntityTable entities, BubbleBlow blow)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(blow);

        _players = players;
        _entities = entities;
        _blow = blow;
    }

    // $220C. The port byte is shifted a bit at a time and each direction handled as it falls out, so
    // the order here is the order there: up, down, left, right, fire. A bit reads clear while its
    // direction is pushed, which is why every test below is against zero.
    //
    // Up is a jmp rather than a jsr in the reference, so a player pushing up does not also walk: the
    // routine leaves for $222B and never reaches the movers. That is why this returns rather than
    // falling through, and it is behaviour rather than an accident of the translation.
    internal void Step(int player, byte port, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        if ((port & Input.Up) == 0)
        {
            Launch(player, port);
            return;
        }

        // Down is shifted out and never tested - $2213 drops it on the way to the left bit.
        PlayerCell cell = PlayerCell.Of(_players.X[player], _players.Y[player]);

        if ((port & Input.Left) == 0)
        {
            Move(player, cell, map, LeftStep);
        }

        if ((port & Input.Right) == 0)
        {
            Move(player, cell, map, RightStep);
        }

        // $2223. The fire arm, and the last bit the byte carries. A jmp rather than a jsr, which
        // costs nothing here - there is nothing after it either way.
        if ((port & Input.Fire) == 0)
        {
            _blow.Blow(player);
        }
    }

    // Whether the player already faces this way. $2277 asks it as a run of cmp and beq, and $22CF as
    // a cmp and bcc followed by two beq, and the two sets of frames are not mirror images of each
    // other, so they are written out as the reference writes them rather than folded together.
    private static bool Faces(bool left, byte frame)
        => left ? frame is 0x04 or 0x05 or 0x0A or 0x0B : frame is < 0x02 or 0x08 or 0x09;

    // Which of the facing frames reach the mover by way of $2294 rather than going straight to the
    // wall check. It is why the walk cycle advances on some pushes and not others.
    private static bool Animates(bool left, byte frame)
        => left ? frame is 0x04 or 0x05 : frame is < 0x02;

    private static int Probe(bool left, bool aligned)
        => left
            ? aligned ? LeftProbeAligned : LeftProbeSplit
            : aligned ? RightProbeAligned : RightProbeSplit;

    // $226B and $22C3, which differ only in their three constants and in which frames count as
    // already facing that way. Everything after that is the shared tail.
    private void Move(int player, in PlayerCell cell, SolidMap map, byte step)
    {
        bool left = step == LeftStep;

        // $25, which $1E6C fills from $8520 before any of this runs: the frame the player is drawn
        // with as the frame begins.
        byte frame = _entities.Frame[player];

        if (!Faces(left, frame))
        {
            Turn(player, left);
            return;
        }

        if (Animates(left, frame))
        {
            Animate(player);
        }

        Advance(player, cell, map, Probe(left, cell.FineY == 0), step);
    }

    // Not facing this way yet, so the push turns the player rather than moving them - but only once
    // the bubble timer says they are not in a bubble. $05C5 sets that byte to $FF for every slot as
    // a level starts, so the ordinary case is that they are not.
    private void Turn(int player, bool left)
    {
        if ((_entities.BubbleTimer[player] & 0x80) == 0)
        {
            return;
        }

        _entities.Frame[player] = left ? FaceLeft : FaceRight;
    }

    // $222B. What starts a jump. The rise counter is set to $0F, which is what PlayerJump counts
    // down, and the stick is latched into the two drift flags for the whole of the jump.
    //
    // Which way a jump drifts is the stick *and* which way the player already faced: pushing left
    // latches left only from a left-facing frame, and right only from a right-facing one. After
    // this nothing reads the stick again until the player lands, because $2483 moves them from the
    // flags alone.
    private void Launch(int player, byte port)
    {
        _entities.RiseCounter[player] = JumpRise;

        // $25, the frame as the frame began - nothing has touched it yet.
        byte frame = _entities.Frame[player];

        bool left = (port & Input.Left) == 0 && frame is >= 0x04 and < 0x08;
        bool right = (port & Input.Right) == 0 && frame is < 0x04;

        _entities.LeftFlag[player] = left ? Latched : (byte)0x00;
        _entities.RightFlag[player] = right ? Latched : (byte)0x00;

        // $225C. The jumping sprite, unless the frame is already one of the high ones.
        if (frame < 0x08)
        {
            _entities.Frame[player] = (byte)(frame | 0x02);
        }

        // $2268 increments $B1, one byte rather than one per player, which nothing translated so far
        // reads. Not translated - see bb-port-plan.md.
    }

    // $2294. One step of the walk cycle every fourth frame, by flipping bit 0 of the sprite - which
    // is $2159, the same toggle the rest of the animation code uses.
    private void Animate(int player)
    {
        _entities.AnimationTimer[player]++;

        if (_entities.AnimationTimer[player] < AnimationPeriod)
        {
            return;
        }

        _entities.AnimationTimer[player] = 0;
        _entities.Frame[player] ^= 0x01;
    }

    // $22A6 to $22C2. The wall check, and then the two pixels.
    private void Advance(int player, in PlayerCell cell, SolidMap map, int probe, byte step)
    {
        // Two ways past the check. Above $2D there is no level to walk into, and a player not
        // sitting on a column boundary is already part way into the next cell, so the cell they are
        // heading for was tested on the frame they entered it.
        bool checkWall = _players.Y[player] >= WallCheckFloor && cell.FineX == 0;

        if (checkWall && cell.Solid(map, probe))
        {
            return;
        }

        // $22B4 reads $63,x here and calls $7C21 when it is set. That routine is Phase 6's, and the
        // byte is clear for a walking player, so the call is not translated - see bb-port-plan.md.
        _players.X[player] = unchecked((byte)(_players.X[player] + step));
    }
}

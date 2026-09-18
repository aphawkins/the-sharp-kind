// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

// $25F1 in entity-interaction.s: the stick, read again while a player is off the ground.
//
// This is the routine that makes a jump steerable, and it is reached from an unlikely place. The
// drift's animation tail at $2515 ends in a jmp to $25F1, so this runs only on the frames the drift
// animates - every second one, and only while the sprite is below $08. That is why a player holding
// right through a jump covers about a pixel and a third a frame rather than one: the drift moves
// them every frame and this moves them again on half of those.
//
// Its two halves at $25FE and $263D are the walk's two movers rewritten. Same question asked of the
// frame, same turn-before-you-move, same probe offsets - but a pixel a step rather than two, an edge
// test that only catches an exact landing on $24 or $F4, and a wall check the walk does not share.
// The frames each half will move on are different too, so they are written out rather than folded
// into the walk's.
internal sealed class PlayerSteer
{
    // $2620 and $265F. The reference compares for equality here, not for less-than, so a player only
    // stops at the edge by arriving on it exactly.
    private const byte LeftEdge = 0x24;
    private const byte RightEdge = 0xF4;

    // $262C and $266B. Below this there is nothing to run into - but see Left and Right, because the
    // reference only consults it on one of the two paths.
    private const byte WallCheckFloor = 0x2D;

    private const int LeftProbeAligned = 0x28;
    private const int LeftProbeSplit = 0x50;
    private const int RightProbeAligned = 0x2B;
    private const int RightProbeSplit = 0x53;

    private const byte FaceLeft = 0x04;
    private const byte FaceRight = 0x00;

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;

    internal PlayerSteer(PlayerTable players, EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);

        _players = players;
        _entities = entities;
    }

    // $25F1. Left is asked first and wins outright: if it is pushed, the right half is never
    // reached, because both of the left half's paths end in an rts.
    internal void Step(int player, byte port, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        // $25F1 opens with jsr $E9B8, so the cell is a fresh one - the arc has already moved the
        // player this frame, and the drift may have moved them again.
        PlayerCell cell = PlayerCell.Of(_players.X[player], _players.Y[player]);

        // $25F5's fire arm reaches $22E8, the bubble release. Phase 5, not translated.
        byte frame = _entities.Frame[player];

        if ((port & Input.Left) == 0)
        {
            if (MovesLeft(frame))
            {
                Left(player, cell, map);
            }
            else
            {
                Turn(player, FaceLeft);
            }

            return;
        }

        if ((port & Input.Right) != 0)
        {
            return;
        }

        if (MovesRight(frame))
        {
            Right(player, cell, map);
        }
        else
        {
            Turn(player, FaceRight);
        }
    }

    // $2604. Frames $04 to $07 and $0A upwards move; below $04, and $08 and $09, turn instead. Not
    // the same set the walk uses, which is why it is spelled out here.
    private static bool MovesLeft(byte frame) => frame is (>= 0x04 and < 0x08) or >= 0x0A;

    // $2641. The mirror, and also not the walk's: below $04, or exactly $08 or $09.
    private static bool MovesRight(byte frame) => frame is < 0x04 or 0x08 or 0x09;

    // $261E to $263C.
    private void Left(int player, in PlayerCell cell, SolidMap map)
    {
        if (_players.X[player] == LeftEdge)
        {
            return;
        }

        bool aligned = cell.FineY == 0;

        // The reference jumps straight past the height test when the fine Y is zero, so a player on
        // a row boundary gets the wall check wherever they are. Only the split path can skip it.
        if (!aligned && _players.Y[player] < WallCheckFloor)
        {
            _players.X[player]--;
            return;
        }

        if (cell.FineX != 0 || !cell.Solid(map, aligned ? LeftProbeAligned : LeftProbeSplit))
        {
            _players.X[player]--;
        }
    }

    // $265D to $267A.
    private void Right(int player, in PlayerCell cell, SolidMap map)
    {
        if (_players.X[player] == RightEdge)
        {
            return;
        }

        bool aligned = cell.FineY == 0;

        if (!aligned && _players.Y[player] < WallCheckFloor)
        {
            _players.X[player]++;
            return;
        }

        if (cell.FineX != 0 || !cell.Solid(map, aligned ? RightProbeAligned : RightProbeSplit))
        {
            _players.X[player]++;
        }
    }

    // $2613 and $2650. A player whose sprite faces the wrong way turns instead of moving, and will
    // not even do that while the bubble timer says they are in a bubble.
    private void Turn(int player, byte facing)
    {
        if ((_entities.BubbleTimer[player] & 0x80) == 0)
        {
            return;
        }

        _entities.Frame[player] = facing;
    }
}

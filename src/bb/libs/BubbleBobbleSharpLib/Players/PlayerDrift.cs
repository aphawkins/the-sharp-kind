// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

// $2483 in entity-interaction.s: how a player moves sideways while they are off the ground.
//
// It is not the walk. On the ground the stick reaches $226B and $22C3 every frame; this reads the
// two flags $8840 and $8868 that $222B latched once, as the jump started, and nothing else reads
// them.
//
// That is not the whole of airborne movement, though. The animation tail below leaves for $25F1,
// which reads the stick again - see PlayerSteer. So a jump's drift is settled at take-off, but the
// player is still steerable on top of it.
//
// Two other differences from the walk are worth having in mind. It moves one pixel a frame rather
// than two, and its animation runs on a period of two rather than four.
//
// Both halves of the arc come here - the rise by way of $23FC and the fall by way of $243A - and a
// landing does not, it leaves for $2519 instead.
internal sealed class PlayerDrift
{
    // $2490 and $24C9. The playfield's edges. Running into one stops the drift rather than the
    // player: the flag is cleared, so the rest of the jump carries straight on.
    private const byte LeftEdge = 0x24;
    private const byte LeftEdgeLimit = 0x25;
    private const byte RightEdge = 0xF4;

    // $24A0 and $24D8. Above this row there is nothing to run into.
    private const byte WallCheckFloor = 0x2D;

    // The cells each side looks at, as $2494 and $24CC name them.
    private const int LeftProbeAligned = 0x28;
    private const int LeftProbeSplit = 0x50;
    private const int RightProbeAligned = 0x2B;
    private const int RightProbeSplit = 0x53;

    // $24FA. Twice as brisk as the walk's.
    private const byte AnimationPeriod = 0x02;

    // $2510. Frames from $08 up are left alone by the animation.
    private const byte AnimationCeiling = 0x08;

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;
    private readonly PlayerSteer _steer;

    internal PlayerDrift(PlayerTable players, EntityTable entities, PlayerSteer steer)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(steer);

        _players = players;
        _entities = entities;
        _steer = steer;
    }

    // $248D. The left flag is looked at first and wins, then the right, then the animation runs
    // whatever happened.
    //
    // The cell wanted here is a fresh one, worked out after the arc has moved the player this frame.
    // $2483 gets it by calling $E9B8 on the way in; the fall reaches $248D directly, having already
    // called it at $2450. Either way the caller has a current cell, so this takes one rather than
    // working it out again.
    //
    // $2483's own prologue reads $61,x and calls $7C21 when it is set. That is Phase 6's, and not
    // translated.
    internal void Step(int player, byte port, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        if ((_entities.LeftFlag[player] & 0x80) != 0)
        {
            Left(player, cell, map);
        }
        else if ((_entities.RightFlag[player] & 0x80) != 0)
        {
            Right(player, cell, map);
        }

        Animate(player, port, map);
    }

    // Whether the way ahead is clear, in the shape $24A6 and $24DE ask it - which is not the shape
    // anyone would write from scratch. The player moves when the cell ahead is open, and *also* when
    // both it and the one past it are solid. Only a solid cell with an open one behind it stops
    // them. It is translated as the reference has it rather than tidied into something simpler.
    private static bool Clear(in PlayerCell cell, SolidMap map, int probe, int beyond)
        => !cell.Solid(map, probe) || cell.Solid(map, beyond);

    // $2490 to $24C4.
    private void Left(int player, in PlayerCell cell, SolidMap map)
    {
        if (_players.X[player] < LeftEdgeLimit)
        {
            _players.X[player] = LeftEdge;
            _entities.LeftFlag[player] = 0x00;
            return;
        }

        int probe = cell.FineY == 0 ? LeftProbeAligned : LeftProbeSplit;

        if (Open(player, cell, map, probe, probe + 1))
        {
            _players.X[player]--;
            return;
        }

        _entities.LeftFlag[player] = 0x00;
    }

    // $24C5 to $24F9. The mirror of the above, except that the second cell it consults is the one
    // before the probe rather than the one after it - the reference does `dey` here and `iny` there.
    private void Right(int player, in PlayerCell cell, SolidMap map)
    {
        if (_players.X[player] >= RightEdge)
        {
            _players.X[player] = RightEdge;
            _entities.RightFlag[player] = 0x00;
            return;
        }

        int probe = cell.FineY == 0 ? RightProbeAligned : RightProbeSplit;

        if (Open(player, cell, map, probe, probe - 1))
        {
            _players.X[player]++;
            return;
        }

        _entities.RightFlag[player] = 0x00;
    }

    // The two ways past the wall check, then the check itself. They are the walk's two as well:
    // nothing to run into above $2D, and a player straddling a column was let in last frame.
    private bool Open(int player, in PlayerCell cell, SolidMap map, int probe, int beyond)
        => _players.Y[player] < WallCheckFloor
            || cell.FineX != 0
            || Clear(cell, map, probe, beyond);

    // $24FA. Every second frame, and only for the frames below $08.
    private void Animate(int player, byte port, SolidMap map)
    {
        _entities.AnimationTimer[player]++;

        if (_entities.AnimationTimer[player] < AnimationPeriod)
        {
            return;
        }

        _entities.AnimationTimer[player] = 0;

        byte frame = _entities.Frame[player];

        if (frame >= AnimationCeiling)
        {
            return;
        }

        _entities.Frame[player] = (byte)(frame ^ 0x01);

        // $2515 leaves for $25F1, the stick read again. It is reached only from here, so a player
        // steers on the frames the drift animates and on no others.
        _steer.Step(player, port, map);
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

// $1CBD, $1E6C and $2162: what happens to a player between one frame and the next.
//
// Everything Phase 4 translated up to now was reached from somewhere else. This is that somewhere.
// $1CBD reads the two ports and walks the eight entity slots from seven down to zero, calling $1E6C
// for every slot whose state byte is not zero; $1E6C works out the cell, copies the sprite frame to
// $25 and dispatches on the state byte; and for a live player - state 1 - the handler is $2162,
// which is where the choice between walking, jumping and falling is actually made.
//
// **Only state 1 is translated.** The jump table at $1E3A has an entry per state, and the rest of
// them are dying, being captured, being freed and the level-99 special - all of which belong to
// phases that have not started. A slot in any other state is left alone here, and named as such
// rather than silently skipped.
//
// **Only the two player slots are driven.** $1CBD's loop covers eight, but slots 2 to 7 are enemies
// and their handlers are Phase 6's.
//
// Two pieces of $2162 are deliberately absent, each because it belongs to a later phase:
//
//   * $2171 to $21BA, the scan over the eighteen entity slots that catches an enemy while the
//     player pushes up. With no enemies in play the scan finds nothing and falls through to $21BD,
//     which is exactly where the arm that skips it goes, so leaving it out changes no outcome yet.
//   * $21F1's `inc $B1` and the rest of what happens to a captured player.
internal sealed class PlayerFrame
{
    // $B2, and the only value the dispatch below knows what to do with. Settled by reading the byte
    // in a running game rather than by inferring it - see docs/bb-port-plan.md.
    internal const byte PlayingState = 0x01;

    // $21CD. The cells under a standing player: their own row has to be solid for them to have
    // something to stand on.
    private const int GroundRowLeft = 0x51;
    private const int GroundRowMiddle = 0x52;
    private const int GroundRowRight = 0x53;

    // $21EE. All five direction bits high is an idle stick, because a bit reads clear while its
    // direction is pushed.
    private const byte StickMask = 0x1F;
    private const byte StickIdle = 0x1F;

    // $21FB. A player standing still with nothing pushed still breathes, on a period of seven.
    private const byte IdlePeriod = 0x07;

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;
    private readonly PlayerMovement _movement;
    private readonly PlayerJump _jump;
    private readonly PlayerFall _fall;
    private readonly BubbleBlow _blow;

    internal PlayerFrame(
        PlayerTable players,
        EntityTable entities,
        PlayerMovement movement,
        PlayerJump jump,
        PlayerFall fall,
        BubbleBlow blow)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(jump);
        ArgumentNullException.ThrowIfNull(fall);
        ArgumentNullException.ThrowIfNull(blow);

        _players = players;
        _entities = entities;
        _movement = movement;
        _jump = jump;
        _fall = fall;
        _blow = blow;
    }

    // $1CBD's loop, over the slots this phase owns. The ports were read before it - Input.Read fills
    // the same two bytes $85E8 and $85E9 hold, indexed the same way, so a player number reaches the
    // right stick without any translation in between.
    internal void Step(ReadOnlySpan<byte> ports, SolidMap map)
    {
        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            Step(player, ports[player], map);
        }
    }

    // $1E6C. The cell first, then the state. $1E6F's copy of $8520 into $25 is not reproduced: every
    // routine that reads $25 reads the frame byte itself here, and a copy taken at the top of the
    // frame would be a second truth to keep in step.
    internal void Step(int player, byte port, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        // $1CDB. A zero state byte is an empty slot - an unjoined second player, or one between
        // lives - and the loop skips it without calling $1E6C at all.
        if (_players.State[player] != PlayingState)
        {
            return;
        }

        Playing(player, port, PlayerCell.Of(_players.X[player], _players.Y[player]), map);
    }

    // Three cells of a row, the third only when the player straddles a column. $21CD asks it in the
    // shape every other probe in Phase 4 asks it.
    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left)
            || cell.Solid(map, middle)
            || (cell.FineX != 0 && cell.Solid(map, right));

    // $2162 and the chain of tests below it. Read it as four questions asked in order: is the player
    // part way through a jump, are they off the ground, have they just walked off the ground, and
    // only then, what is the stick doing.
    private void Playing(int player, byte port, in PlayerCell cell, SolidMap map)
    {
        // $2162. Before any of the movement, the blow: a bubble timer that is not negative means a
        // blow is part way through, and $2301 runs one frame of it. The timer as it is now is what
        // $2301 branches on, so it is read before the call rather than inside it.
        byte blowTimer = _entities.BubbleTimer[player];

        if ((blowTimer & 0x80) == 0)
        {
            _blow.Step(player, blowTimer, cell);
        }

        // $21BD. The rise counter parked at $FF is the idle a player on the ground sits at, and
        // anything else means an arc is running, so the jump owns the frame.
        if ((_entities.RiseCounter[player] & 0x80) == 0)
        {
            _jump.Step(player, port, cell, map);
            return;
        }

        // $21C5. Off the ground without an arc - which is how walking off a ledge leaves a player.
        if ((_entities.GroundState[player] & 0x80) == 0)
        {
            _fall.Step(player, port, cell, map);
            return;
        }

        // $21CD. Whether there is still something underneath, asked only on a row boundary: a player
        // part way down a row was standing a moment ago and the check is skipped.
        if (cell.FineY == 0 && !Blocked(cell, map, GroundRowLeft, GroundRowMiddle, GroundRowRight))
        {
            // $21E5. Out of $FF and into a fall, on the same frame.
            _entities.GroundState[player]++;
            _fall.Step(player, port, cell, map);
            return;
        }

        // $21EB. Anything at all pushed and the stick drives the walk; nothing pushed and the player
        // stands there.
        if ((port & StickMask) != StickIdle)
        {
            _movement.Step(player, port, map);
            return;
        }

        Idle(player, port, map);
    }

    // $21F3 to $2209. A player with an idle stick and no bubble round them animates on a period of
    // seven - $2159's toggle again, at a third of the walk's pace.
    private void Idle(int player, byte port, SolidMap map)
    {
        // $21F3. In a bubble the stick still reaches the movers, which is how a bubbled player is
        // pushed about.
        if ((_entities.BubbleTimer[player] & 0x80) == 0)
        {
            _movement.Step(player, port, map);
            return;
        }

        _entities.AnimationTimer[player]++;

        // $2200's cmp is for equality, not for less-than: a counter that somehow passed seven would
        // run all the way round rather than resetting.
        if (_entities.AnimationTimer[player] != IdlePeriod)
        {
            return;
        }

        // $2159.
        _entities.Frame[player] ^= 0x01;
        _entities.AnimationTimer[player] = 0;
    }
}

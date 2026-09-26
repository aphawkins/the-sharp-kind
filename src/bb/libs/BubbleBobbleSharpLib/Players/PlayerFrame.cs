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
// **States 1, $0E and $0F are translated.** The jump table at $1E3A has an entry per state: 1 is
// $2162 below, $0E is PlayerDying ($28A5) and $0F is $28E8, a bare `rts`. Any other state reaching
// $1E6C throws, by the loud-gap rule.
//
// **$1D03 can run $1E6C twice.** A slot whose $8728 is not zero gets an extra call on every other
// pass, before the ordinary one. For a player that is item effect 0's flash, and it is also why the
// first pass of a death can step the death twice: $28A5 clears $8728, but only once it is running.
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

    // $1D6F and $28F8. Dying, and dead.
    internal const byte DyingState = 0x0E;
    internal const byte DeadState = 0x0F;

    // $1D0A. Bit 1 of $08.
    private const byte AngerFrames = 0x02;

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

    private readonly EntityTable _entities;
    private readonly PlayerMovement _movement;
    private readonly PlayerJump _jump;
    private readonly PlayerFall _fall;
    private readonly BubbleBlow _blow;
    private readonly PlayerDying _dying;

    internal PlayerFrame(
        EntityTable entities,
        PlayerMovement movement,
        PlayerJump jump,
        PlayerFall fall,
        BubbleBlow blow)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(jump);
        ArgumentNullException.ThrowIfNull(fall);
        ArgumentNullException.ThrowIfNull(blow);

        _entities = entities;
        _movement = movement;
        _jump = jump;
        _fall = fall;
        _blow = blow;
        _dying = new(entities);
    }

    // $1CBD's loop, over the two player slots. The ports were read before it - Input.Read fills the
    // same two bytes $85E8 and $85E9 hold, indexed the same way, so a player number reaches the right
    // stick without any translation in between. counter is $08, as EnemyFrame takes it.
    internal void Step(ReadOnlySpan<byte> ports, byte counter, SolidMap map)
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            Slot(player, ports[player], counter, map);
        }
    }

    // $1E6C. The cell first, then the state. $1E6F's copy of $8520 into $25 is not reproduced: every
    // routine that reads $25 reads the frame byte itself here, and a copy taken at the top of the
    // frame would be a second truth to keep in step.
    internal void Step(int player, byte port, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        switch (_entities.State[player])
        {
            case 0:
                return;
            case PlayingState:
                Playing(player, port, PlayerCell.Of(_entities.X[player], _entities.Y[player]), map);
                return;
            case DyingState:
                _dying.Step(player);
                return;
            case DeadState:
                // $28E8.
                return;
            default:
                throw new NotSupportedException(
                    $"slot {player}'s state ${_entities.State[player]:X2} at $1E6C is not translated");
        }
    }

    // $1CDB to $1D21, for a player slot. $85C0 is $FF for a player from the start, so the drop at
    // $1CA0 is never taken, and the `SESSION` test at $1CF7 is for slots 2 upwards.
    internal void Slot(int player, byte port, byte counter, SolidMap map)
    {
        // $1CDB. A zero state byte is an empty slot - an unjoined second player, or one between
        // lives - and the loop skips it without calling $1E6C at all.
        if (_entities.State[player] == 0)
        {
            return;
        }

        // $1D03. The flash's extra call, on the passes where bit 1 of $08 is clear.
        if (_entities.FlashTimer[player] != 0 && (counter & AngerFrames) == 0)
        {
            Step(player, port, map);
        }

        // $1D11. Nothing translated sets a player's $8638, and $1E87 would send a player to $EB0F, the
        // enemy walker.
        if (_entities.HoldTimer[player] != 0)
        {
            throw new NotSupportedException($"slot {player} with $8638 set at $1D11 is not translated");
        }

        Step(player, port, map);
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

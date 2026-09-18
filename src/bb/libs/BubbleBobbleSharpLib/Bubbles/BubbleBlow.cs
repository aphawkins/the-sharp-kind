// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Bubbles;

// $22E8 and $2301 in entity-interaction.s: a player pressing fire, and the six frames that follow.
//
// Pressing fire does not make a bubble. $22E8 only starts a clock - it puts $06 in the bubble timer
// and latches which way the player faces - and $2301, called from the top of every later frame,
// runs that clock down. The bubble is made part way through, on the frame the timer reads $04, and
// the last frame puts the player's sprite back the way it was. So the button and the bubble are
// three frames apart, and a player who dies in between never makes one.
//
// The reference heads $22E8 "bubble release timer/animation" and $2301 "continue bubble animation
// update", and for once both are right.
//
// Three pieces of $2301 are left out, each gated by a byte that a level start leaves in the state
// that skips it, and each named here rather than skipped silently:
//
//   * $23B8's arm on $A783, which turns the new bubble into type $44. Level setup puts $FF in
//     $A783 for both players, and the arm wants it positive. It belongs to Phase 7.
//   * $23D7's sound trigger on $65, which $05C5 leaves at zero. Phase 8.
//   * $23DE's arm on $37C7, which rewrites the bubble's flag byte. Level setup zeroes it and the
//     arm wants it negative. Phase 7 again.
internal sealed class BubbleBlow
{
    // $2352. What $2321 puts in the slot it finds.
    internal const byte BubbleType = 0x16;

    // $22EE. Six frames of blowing, counted down by $2301.
    internal const byte BlowFrames = 0x06;

    // $231B. The timer value the bubble is made on - halfway, with the player's mouth at its widest.
    internal const byte SpawnTimer = 0x04;

    // $E9B8's own origins, subtracted again here. $232A works the bubble's cell out from the
    // player's pixel position rather than from the cell $1E6C already has, so the two subtractions
    // are written out rather than taken from PlayerCell.
    private const byte LeftEdge = 0x14;
    private const byte TopEdge = 0x15;

    // $2385. Blowing while facing right puts the bubble a column further right, because the sprite
    // is drawn from its left edge and the player's mouth is at the far end of it. There is no
    // matching case facing left: the mouth is already at the sprite's origin.
    private const byte FacingRightSprite = 0x09;

    // $239C. A player low enough in their cell blows into the row below, and only while they are
    // off the ground - $87A0 negative is the idle a standing player sits at.
    private const int LowInCell = 0x04;

    // $ACC4, indexed by the timer. Entry 0 is never read: a timer of zero takes $2301's other arm.
    private static readonly byte[] s_blowSprite = [0x00, 0x08, 0x08, 0x09, 0x09, 0x08, 0x08];

    // $A77B, $A77D, $A77F and $A781, one byte per player and the same byte in both. $2364 copies
    // three of them into the new bubble and the fourth into the player's reload.
    private static readonly byte[] s_reload = [0x08, 0x08];
    private static readonly byte[] s_state = [0x88, 0x88];
    private static readonly byte[] s_variant = [0x04, 0x04];
    private static readonly byte[] s_enemyType = [0x04, 0x04];

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;

    internal BubbleBlow(PlayerTable players, EntityTable entities, ObjectTable objects)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);

        _players = players;
        _entities = entities;
        _objects = objects;
    }

    // $0A28 in game-loop.s, which is the only thing that ever clears a reload. It runs once a frame
    // and before the entity update, so a reload set on one frame is a frame shorter than the eight
    // it was given.
    //
    // It belongs here rather than with the rest of the game loop because nothing else reads these
    // two bytes. game-loop.s calls them invincibility timers; see PlayerTable.Reload.
    internal void Tick()
    {
        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            if (_players.Reload[player] != 0)
            {
                _players.Reload[player]--;
            }
        }
    }

    // $22E8, reached from the fire arm of $220C on the ground and of $25F1 in the air. Both are the
    // same call, so a player blows whether they are walking or part way through a jump.
    internal void Blow(int player)
    {
        // $22E8. Still reloading.
        if (_players.Reload[player] != 0)
        {
            return;
        }

        // $22ED. A blow already running, or a player inside a bubble - the same byte says both, and
        // negative is neither.
        if ((_entities.BubbleTimer[player] & 0x80) == 0)
        {
            return;
        }

        _entities.BubbleTimer[player] = BlowFrames;

        // $22F4. Bit 2 of the sprite is the facing, and halving it gives the 0 or 2 that $2301
        // doubles back. Kept as the reference keeps it, because $230E adds it to the table.
        _players.BlowFacing[player] = (byte)((_entities.Frame[player] & 0x04) >> 1);
    }

    // $2301, called from $2165 on every frame the timer is not negative, with the timer as it was
    // before this frame. The decrement happens here, on both arms.
    internal void Step(int player, byte timer, in PlayerCell cell)
    {
        // $2303. The last frame: the sprite goes back to the plain facing frame, and the timer goes
        // from zero to $FF, which is the idle everything else tests for.
        if (timer == 0)
        {
            _entities.Frame[player] = (byte)(_players.BlowFacing[player] << 1);
            _entities.BubbleTimer[player]--;
            return;
        }

        // $230E.
        _entities.Frame[player] = (byte)(_players.BlowFacing[player] + s_blowSprite[timer]);
        _entities.BubbleTimer[player]--;

        // $231B. One frame out of the six makes the bubble.
        if (timer != SpawnTimer)
        {
            return;
        }

        Spawn(player, cell);
    }

    // $2321 to $23E9. Find a slot, fill it from the player, and start the player reloading.
    private void Spawn(int player, in PlayerCell cell)
    {
        int slot = _objects.FindFree();

        if (slot < 0)
        {
            return;
        }

        // $232A. The horizontal half, which cannot fail: the subtraction wraps like the 6502's.
        byte x = _players.X[player];
        byte acrossX = (byte)(x - LeftEdge);

        _objects.X[slot] = x;
        _objects.SubX[slot] = (byte)((acrossX & 0x06) >> 1);
        _objects.Column[slot] = (byte)(acrossX >> 3);

        // $2341. The vertical half, which can fail: a player above the top of the playfield borrows
        // from the subtraction and $2349 abandons the whole spawn. The four bytes already written
        // are left where they are, exactly as the 6502 leaves them - the slot is still free,
        // because nothing has put a type in it yet.
        byte y = _players.Y[player];
        int downY = y - TopEdge;

        _objects.Y[slot] = y;

        if (downY < 0)
        {
            return;
        }

        _objects.SubY[slot] = (byte)(downY & 0x06);
        _objects.Row[slot] = (byte)(downY >> 3);

        // $2352.
        _objects.Type[slot] = BubbleType;
        _objects.Flags[slot] = 0x7D;
        _objects.Behaviour[slot] = 0x7D;

        // $2364. The reload starts here rather than at the button, so its eight frames are measured
        // from the bubble and a player holding fire is on a fourteen-frame cycle.
        _players.Reload[player] = s_reload[player];
        _objects.EnemyType[slot] = s_enemyType[player];
        _objects.State[slot] = s_state[player];
        _objects.Variant[slot] = s_variant[player];

        Place(player, slot, cell);
    }

    // $2385 to $23B7. Two adjustments to where the bubble landed, and the direction byte that falls
    // out of the first of them.
    private void Place(int player, int slot, in PlayerCell cell)
    {
        if (_entities.Frame[player] == FacingRightSprite)
        {
            _objects.Column[slot]++;
            _objects.X[slot] += 8;
            _objects.Direction[slot] = 0x00;
        }
        else
        {
            _objects.Direction[slot] = 0x80;
        }

        // $2399. Both halves have to hold: a standing player never gets this, whatever their cell.
        if ((_entities.RiseCounter[player] & 0x80) != 0 || cell.FineY < LowInCell)
        {
            return;
        }

        _objects.Row[slot]++;
        _objects.Y[slot] += 8;
    }
}

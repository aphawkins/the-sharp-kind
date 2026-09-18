// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

// $23EA in entity-interaction.s: the jump, the fall and the landing.
//
// Found the same way the walk was, and misnamed the same way. The reference heads this routine
// "bubble ascent (captured enemy rising)". A store checkpoint on $C2 with up held separates it into
// three - $23FC rising, $243A falling, $247E landing - and none of the three fires while a player
// stands still. It is a player jumping.
//
// One table drives both halves of the arc. $ACCD holds sixteen deltas, and the rise reads it with a
// counter running down while the fall reads it with a counter running up, which is what makes a jump
// slow as it tops out and gather speed as it comes back. The reference calls it the "fall speed
// table", which is half the story.
//
// Nothing here decides *to* jump. Reaching this routine at all is the caller's business at $21BD,
// and what sets the rise counter in the first place is $222B, which is not translated - see
// bb-port-plan.md.
internal sealed class PlayerJump
{
    // $2423. Sixteen frames of falling and the fall is over.
    private const byte FallEnd = 0x10;

    // The playfield wraps in Y, and the two halves of the arc each check their own end of it.
    private const byte TopWrap = 0x05;
    private const byte BottomWrap = 0xF5;
    private const byte WrapStep = 0xF0;

    // $2474 and $243C. Rows are eight pixels from $2D, which is what a landing snaps to.
    private const byte GridBase = 0x2D;

    // $243C. Above this there is no floor to land on.
    private const byte LandCheckFloor = 0x1F;

    // $2453 and $2460: the row a falling player must find clear, then the row it lands on.
    private const int ClearRowLeft = 0x29;
    private const int ClearRowMiddle = 0x2A;
    private const int ClearRowRight = 0x2B;
    private const int FloorRowLeft = 0x51;
    private const int FloorRowMiddle = 0x52;
    private const int FloorRowRight = 0x53;

    // $ACCD. Sixteen entries, and the only reason a jump has a shape.
    private static readonly byte[] s_arc =
    [
        0x00, 0x01, 0x01, 0x02, 0x02, 0x02, 0x03, 0x03,
        0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x04, 0x04,
    ];

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;
    private readonly PlayerDrift _drift;
    private readonly PlayerLanding _landing;

    internal PlayerJump(PlayerTable players, EntityTable entities, PlayerDrift drift, PlayerLanding landing)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(landing);

        _players = players;
        _entities = entities;
        _drift = drift;
        _landing = landing;
    }

    // $23EA. The routine is entered with the rise counter already in the accumulator, so the first
    // thing it does is branch on whether that counter has run out: rising while it has not, falling
    // once it has.
    //
    // The cell is the one worked out at the top of the frame, before anything moved. The landing
    // check wants both that one and a fresh one, and the difference between them matters.
    //
    // The caller at $21BD only arrives here when the rise counter is not negative, so the counter is
    // 0 to 15 and indexes the table. $05C5 parks it at $FF between jumps, which is why that check
    // belongs to the caller rather than here.
    internal void Step(int player, byte port, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte rise = _entities.RiseCounter[player];

        if (rise != 0)
        {
            Rise(player, port, map, rise);
            return;
        }

        Fall(player, port, cell, map);
    }

    // Three cells of a row, with the third read only when the player straddles a column - $23 being
    // set is what says they do.
    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left)
            || cell.Solid(map, middle)
            || (cell.FineX != 0 && cell.Solid(map, right));

    // $23EA to $2400. The counter is read, then decremented, so the delta used is the one before the
    // decrement - the reference does dec then tay, and tay carries the value the accumulator was
    // holding all along rather than the one just written to memory.
    private void Rise(int player, byte port, SolidMap map, byte rise)
    {
        _entities.RiseCounter[player] = unchecked((byte)(rise - 1));

        byte y = unchecked((byte)(_players.Y[player] - s_arc[rise]));

        if (y < TopWrap)
        {
            y = unchecked((byte)(y + WrapStep));
        }

        _players.Y[player] = y;

        // $23FC leaves for $2483, which works the player sideways from the drift flags. The cell it
        // wants is a fresh one: $2483 calls $E9B8 on the way in, after this frame's move.
        _drift.Step(player, port, PlayerCell.Of(_players.X[player], y), map);
    }

    // $2401 to $243B. The first frame of a fall tidies the sprite and zeroes the counter; every
    // frame after that reads the counter, ends the fall at sixteen, and otherwise steps the arc.
    private void Fall(int player, byte port, in PlayerCell cell, SolidMap map)
    {
        byte fall = _entities.FallCounter[player];

        if ((fall & 0x80) != 0)
        {
            Settle(player);
            fall = 0x00;
        }
        else if (fall == FallEnd)
        {
            // $2423. A fall that runs out of table ends the arc the same way a landing does, with
            // the cell the frame began with - nothing has recomputed it on this path.
            _landing.Step(player, cell, map);
            return;
        }

        _entities.FallCounter[player] = unchecked((byte)(fall + 1));

        byte y = unchecked((byte)(_players.Y[player] + s_arc[fall]));

        if (y >= BottomWrap)
        {
            y = unchecked((byte)(y - WrapStep));
        }

        _players.Y[player] = y;

        if (Land(player, cell, map, y, out PlayerCell landed))
        {
            // $2480 leaves for $2519, and a landing does not drift. It hands on the cell $2450
            // worked out, which is the one the landing probes used.
            _landing.Step(player, landed, map);
            return;
        }

        // $2453 and $2470 both leave for $248D, the same drift $23FC reaches through $2483. The
        // difference between the two entry points is the prologue, which is not translated.
        _drift.Step(player, port, PlayerCell.Of(_players.X[player], y), map);
    }

    // $2404 to $2422. Frames 2, 3, 6 and 7 are masked down as a fall begins; the rest are left
    // alone. The reference asks it as four compares that interleave their branches, and the two
    // ranges that fall through are the only ones that reach the mask.
    private void Settle(int player)
    {
        byte frame = _entities.Frame[player];

        if (frame is (>= 0x02 and < 0x04) or (>= 0x06 and < 0x08))
        {
            _entities.Frame[player] = (byte)(frame & 0x05);
        }

        _entities.FallCounter[player] = 0x00;
    }

    // $243C to $2482. Whether this frame of the fall ends on a floor, and where that puts the player.
    // True means landed, which is the one way out of a fall that does not drift.
    private bool Land(int player, in PlayerCell cell, SolidMap map, byte y, out PlayerCell landed)
    {
        landed = default;

        if (y < LandCheckFloor)
        {
            return false;
        }

        // The fine Y here is the one from the top of the frame, because $E9B8 has not run again yet.
        // A player who has not yet crossed into the next row keeps falling.
        if ((unchecked((byte)(y - GridBase)) & 0x07) >= cell.FineY)
        {
            return false;
        }

        // $2450, jsr D_E9B8: the probes below read the cell the player has just moved into, not the
        // one they started the frame in.
        landed = PlayerCell.Of(_players.X[player], y);

        if (Blocked(landed, map, ClearRowLeft, ClearRowMiddle, ClearRowRight))
        {
            return false;
        }

        if (!Blocked(landed, map, FloorRowLeft, FloorRowMiddle, FloorRowRight))
        {
            return false;
        }

        _players.Y[player] = unchecked((byte)((unchecked((byte)(y - GridBase)) & 0xF8) + GridBase));
        return true;
    }
}

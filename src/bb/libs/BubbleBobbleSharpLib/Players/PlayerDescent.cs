// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

// $EB48 in entity-system.s: falling at a flat two pixels a frame until there is something underneath.
//
// This is the plain descent, not the jump's arc. The arc reads a table and slows at the top; this
// does not, and a thing on it falls at the same speed the whole way down and wraps from the bottom of
// the playfield back to the top.
//
// It is shared. Watched in VICE it runs for entity slots rather than the players - thirty traps in a
// run, every one of them slot $02 or $03, with their Y climbing by twos from $15 downwards. The
// player reaches it by one route only, from $2575, when a landing finds no floor. That route is read
// from the source rather than watched happening, so see bb-port-plan.md before relying on it.
//
// **The self-modifying code is deliberately not reproduced.** $EB34, $EB3F and $EBB8 each write a
// different continuation into $EB94 and $EBB5 and then fall into this body, so what they really
// choose between is where to go afterwards: the entity animation step at $EB0F, $EE4A reached past a
// BIT, or a bare rts. Phase 4 arrives through $EB3F, whose continuation is $EB0F both ways out.
// $EB0F is the generic entity animation and wants two arrays this port has no reader for yet, so it
// is left to Phase 6 and this class stops where the continuation begins.
internal sealed class PlayerDescent
{
    // $EB51. Flat, and twice the arc's slowest step.
    private const byte Drop = 0x02;

    // $EB56 and $EB5A. The playfield wraps: exactly $F5 comes back at $15. It is an equality test in
    // the reference, not a comparison, so a thing that steps over $F5 does not wrap.
    private const byte BottomWrap = 0xF5;
    private const byte TopRow = 0x15;

    // $EB5F. Above this there is no floor to find.
    private const byte GroundCheckFloor = 0x1F;

    // $EB62. Rows are eight pixels from $2D, and the ground is only looked for on a row boundary.
    private const byte GridBase = 0x2D;

    // $EB69 and $EB7D: the row the thing is in, which has to be clear, then the row below it, which
    // has to be solid. One row lower than the landing's pair, because the landing works from a cell
    // recomputed after its move and this one does not.
    private const int ClearRowLeft = 0x51;
    private const int ClearRowMiddle = 0x52;
    private const int ClearRowRight = 0x53;
    private const int FloorRowLeft = 0x79;
    private const int FloorRowMiddle = 0x7A;
    private const int FloorRowRight = 0x7B;

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;

    internal PlayerDescent(PlayerTable players, EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);

        _players = players;
        _entities = entities;
    }

    // $EB48. The cell is the one the frame started with: nothing in this routine calls $E9B8, so the
    // probes read where the thing was before this step moved it.
    internal void Step(int player, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte y = unchecked((byte)(_players.Y[player] + Drop));
        _players.Y[player] = y;

        if (y == BottomWrap)
        {
            _players.Y[player] = TopRow;
            return;
        }

        if (y < GroundCheckFloor)
        {
            return;
        }

        if ((unchecked((byte)(y - GridBase)) & 0x07) != 0)
        {
            return;
        }

        if (Blocked(cell, map, ClearRowLeft, ClearRowMiddle, ClearRowRight))
        {
            return;
        }

        if (!Blocked(cell, map, FloorRowLeft, FloorRowMiddle, FloorRowRight))
        {
            return;
        }

        // $EB91. Back to $FF, which is what the rest of the port reads as standing on something.
        _entities.GroundState[player]--;
    }

    // Three cells of a row, the third only when the thing straddles a column.
    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left)
            || cell.Solid(map, middle)
            || (cell.FineX != 0 && cell.Solid(map, right));
}

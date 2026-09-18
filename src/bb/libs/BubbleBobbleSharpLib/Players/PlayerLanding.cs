// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

// $2519 in entity-interaction.s: the end of a jump, however it ends.
//
// The reference heads it "bubble reached top - check for pop". It is neither. Both ways out of an
// arc arrive here - the landing at $2480, and the fall running out of table at $2423 - and one
// landing caught in VICE says what it does: both arc counters go back to $FF, X is squared up to an
// even column, and the ground is checked one last time.
//
// The X alignment is the part worth knowing about, because nothing else hints at it. A landing tidies
// the player on both axes: $247E puts Y on the row grid and this puts X on an even column. Which way
// it rounds follows which way they face - right-facing rounds up, left-facing rounds down - so a
// player is always squared up in the direction they were travelling.
internal sealed class PlayerLanding
{
    // $2530 and $2538. Columns are two pixels; a landing ends on one of them.
    private const byte ColumnMask = 0xFE;

    // $2541 and $2555: the row that has to be clear, then the row that has to be there.
    private const int ClearRowLeft = 0x29;
    private const int ClearRowMiddle = 0x2A;
    private const int ClearRowRight = 0x2B;
    private const int FloorRowLeft = 0x51;
    private const int FloorRowMiddle = 0x52;
    private const int FloorRowRight = 0x53;

    // $256B. The reference compares SUBFLG, which counts levels from zero, so this is the
    // ninety-second level rather than the ninety-first the comment there claims.
    private const int SpecialLevel = 0x5B;

    // $2571. On that level alone, a player at this height is left where they are.
    private const byte SpecialHeight = 0xDD;

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;
    private readonly PlayerDescent _descent;

    internal PlayerLanding(PlayerTable players, EntityTable entities, PlayerDescent descent)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(descent);

        _players = players;
        _entities = entities;
        _descent = descent;
    }

    // $2519.
    //
    // The cell comes from the caller and is deliberately not worked out here. Nothing between the
    // last $E9B8 and the probes below calls it again, so they read the player as they were before
    // this routine squares X up - which is a pixel earlier, and can be a different column.
    internal void Step(int player, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        // $2519. Both counters back to the idle the caller at $21BD tests for.
        _entities.RiseCounter[player] = 0xFF;
        _entities.FallCounter[player] = 0xFF;

        Square(player);

        // $2541. The cell the player is standing in has to be clear and the one below it solid; any
        // other answer means there is nothing to stand on.
        bool standing = !Blocked(cell, map, ClearRowLeft, ClearRowMiddle, ClearRowRight)
            && Blocked(cell, map, FloorRowLeft, FloorRowMiddle, FloorRowRight);

        if (standing)
        {
            return;
        }

        // $2569. One level is excepted, at one height.
        if (map.Number - 1 == SpecialLevel && _players.Y[player] == SpecialHeight)
        {
            return;
        }

        // $2575. Nothing underneath, so the player starts falling again, and $2578 leaves for
        // $EB3F with the same cell still in hand.
        _entities.GroundState[player]++;
        _descent.Step(player, cell, map);
    }

    // Three cells of a row, the third only when the player straddles a column.
    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left)
            || cell.Solid(map, middle)
            || (cell.FineX != 0 && cell.Solid(map, right));

    // $2524 to $2540. Frames $04 to $09 round down, the rest round up.
    //
    // The reference reaches the same answer by a longer road: $2530 rounds down, and then falls
    // through into the rounding up when what it stored was zero. Zero rounded up is zero again, so
    // the fall-through changes nothing and is not reproduced.
    private void Square(int player)
    {
        byte x = _players.X[player];

        _players.X[player] = _entities.Frame[player] is >= 0x04 and < 0x0A
            ? (byte)(x & ColumnMask)
            : unchecked((byte)((x + 1) & ColumnMask));
    }
}

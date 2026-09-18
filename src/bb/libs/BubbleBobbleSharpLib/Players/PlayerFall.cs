// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

// $257C in entity-interaction.s: a player falling with no jump behind it.
//
// This is what a player does after walking off a ledge, and it is *not* the jump's fall. The arc at
// $23EA reads $ACCD and gathers speed; this drops a flat two pixels a frame, the way $EB48 does.
//
// It is not $EB48 either, though the two are close enough to be worth naming the differences.
// Both probe the same two rows - $51 and $52 have to be clear, $79 and $7A solid - and both wrap
// $F5 round to $15. Where they part:
//
// the height below which the ground is not looked for is $26 here and $1F there, landing here
// squares X up to an even column where $EB48 leaves X alone, and the tail is different - $EB48
// leaves for whichever continuation its entry point wrote into itself, while this one runs its own
// animation counter and, every second frame, the stick.
//
// That last one is why a player who walks off a ledge can still steer. $257C's L_25E2 falls
// straight into $25F1, which is the same routine the drift's tail jumps to - so the steer is reached
// from two places, not the one PlayerSteer's note claims.
//
// One thing it does *not* share with the drift's tail: there is no frame toggle here. L_25E2
// increments the counter and resets it, and the animation such as it is comes out of the steer.
internal sealed class PlayerFall
{
    // $2580. Flat, like $EB48's and twice the arc's slowest step.
    private const byte Drop = 0x02;

    // $2586 and $258A. An equality test in the reference, so a player who steps over $F5 does not
    // wrap - the same reading PlayerDescent takes of the same pair.
    private const byte BottomWrap = 0xF5;
    private const byte TopRow = 0x15;

    // $258C. Above this row there is no floor to find. $EB48 uses $1F for the same test.
    private const byte GroundCheckFloor = 0x26;

    // $2591. Rows are eight pixels from $2D, and the ground is only looked for on a row boundary.
    private const byte GridBase = 0x2D;

    // $2597 and $25AB: the row the player is in, which has to be clear, then the row below it, which
    // has to be solid.
    private const int ClearRowLeft = 0x51;
    private const int ClearRowMiddle = 0x52;
    private const int ClearRowRight = 0x53;
    private const int FloorRowLeft = 0x79;
    private const int FloorRowMiddle = 0x7A;
    private const int FloorRowRight = 0x7B;

    // $25D1 and $25D8. Columns are two pixels; a landing ends on one of them.
    private const byte ColumnMask = 0xFE;

    // $25E2. Every second frame, and the reference's cmp is against two.
    private const byte AnimationPeriod = 0x02;

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;
    private readonly PlayerSteer _steer;

    internal PlayerFall(PlayerTable players, EntityTable entities, PlayerSteer steer)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(steer);

        _players = players;
        _entities = entities;
        _steer = steer;
    }

    // $257C. The cell is the one the frame started with: nothing here calls $E9B8, so the probes
    // read where the player was before this step moved them. The steer at the end calls it for
    // itself, which is why it takes no cell.
    internal void Step(int player, byte port, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte y = unchecked((byte)(_players.Y[player] + Drop));
        _players.Y[player] = y;

        // $2586. The wrap is the whole of the frame - there is no animation after it.
        if (y == BottomWrap)
        {
            _players.Y[player] = TopRow;
            return;
        }

        if (Landed(y, cell, map))
        {
            Land(player);
            return;
        }

        Animate(player, port, map);
    }

    // $258C to $25BE. Three ways to be still falling before the probes are even reached, and then
    // the pair of rows: the player's own has to be clear and the one under it solid.
    private static bool Landed(byte y, in PlayerCell cell, SolidMap map)
        => y >= GroundCheckFloor
            && (unchecked((byte)(y - GridBase)) & 0x07) == 0
            && !Blocked(cell, map, ClearRowLeft, ClearRowMiddle, ClearRowRight)
            && Blocked(cell, map, FloorRowLeft, FloorRowMiddle, FloorRowRight);

    // Three cells of a row, the third only when the player straddles a column.
    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left)
            || cell.Solid(map, middle)
            || (cell.FineX != 0 && cell.Solid(map, right));

    // $25BF. On the ground, and squared up in the direction the player faces.
    //
    // The frames are *not* the sets $2519 uses, which is the reason this is written out rather than
    // shared with PlayerLanding. There, $04 to $09 round down and the rest round up. Here $04 to
    // $07 and $0A upwards round down, and below $04 and the pair $08, $09 round up.
    private void Land(int player)
    {
        // $25BF. A dec, which takes the byte back to $FF - the value the rest of the port reads as
        // standing on something.
        _entities.GroundState[player]--;

        byte x = _players.X[player];
        byte frame = _entities.Frame[player];

        _players.X[player] = frame is (>= 0x04 and < 0x08) or >= 0x0A
            ? (byte)(x & ColumnMask)
            : unchecked((byte)((x + 1) & ColumnMask));
    }

    // $25E2, which falls into $25F1 rather than jumping to it. The counter is the only thing this
    // half does on its own; the frame the player is drawn with is the steer's business.
    private void Animate(int player, byte port, SolidMap map)
    {
        _entities.AnimationTimer[player]++;

        if (_entities.AnimationTimer[player] < AnimationPeriod)
        {
            return;
        }

        _entities.AnimationTimer[player] = 0;
        _steer.Step(player, port, map);
    }
}

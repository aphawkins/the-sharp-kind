// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $1473 to $1577 in player-sprites.s: Baron Von Blubba, who comes for a player who takes too long.
//
// **Not the routine the reference names for him.** special-enemies.s heads $10D3 as the Baron and
// type $44 as his. Watched in VICE on 2026-09-23, on level 1 with the enemies cleared, no type $44
// appeared. When the hurry-up ran out, $1677 put type $2E in ObjectTable slot 0, and twenty seconds
// later player 1 was in state $0E with no enemy in any slot. $0D4E sends types $2E and $30 to $1473,
// which the reference calls player_death_handler. That is the Baron. $44 is a bubble type: $23D5
// writes it.
//
// **There is one Baron per player, in the ObjectTable slot of the player's own number.** Slot 0 is
// type $2E and chases player 1, and slot 1 is type $30 and chases player 2. That is why $1473 reads
// $B2, $BA and $C2 with the ObjectTable slot as the index.
//
// He moves on the map's cells, not in pixels: one cell a pass, in a run along one axis and then a run
// along the other, and he pauses thirty passes between each pair of runs. He passes through walls.
// Anything playing within sixteen pixels of him dies. When his own player dies, he goes.
//
// **Not here:** $1621, which puts him in play as the hurry-up runs out, and $1490's store of $32 in
// $2B, the level timer's frame count. Both are the level timer's, which is Phase 7's.
internal sealed class Baron
{
    // $1475 and $156E. A player dying.
    private const byte Dying = 0x0E;

    // $1479. What the slot becomes when the Baron goes.
    private const byte GoneType = 0x3A;

    // $148A and $148E. The two Barons' types.
    private const byte PlayerOneType = 0x2E;
    private const byte PlayerTwoType = 0x30;

    // $1496 to $14A2. The caller changes $152B's two compares to $18 and $0E: a player in state $18,
    // or in any live state below $0E, can be caught.
    private const byte CatchableState = 0x18;
    private const byte CatchLimit = 0x0E;

    // $1558 and $1568. Sixteen pixels in each axis.
    private const byte CatchRange = 0x10;

    // $152B and $1533. A cell's corner in pixels, as the Baron's sprite has it.
    private const byte ColumnOrigin = 0x18;
    private const byte RowOrigin = 0x15;

    // $14D9 and $14EF. The player's cell, as $E9B8 has it without its biases.
    private const byte LeftEdge = 0x14;
    private const byte TopEdge = 0x15;

    // $14C6. Thirty passes between each pair of runs.
    private const byte Pause = 0x1E;

    // $14E4 and $14E8. The Baron's sprite faces the player.
    private const byte FacingRight = 0x00;
    private const byte FacingLeft = 0x02;

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;

    // $A775 and $E765, two bytes each, one per Baron. game-tables-1.s starts the first at $16 and $00
    // and sprite-composer.s starts the second at $02 and $00. Nothing resets either, so a Baron takes
    // over what the last one in his slot left.
    private readonly byte[] _pause = [0x16, 0x00];
    private readonly byte[] _facing = [0x02, 0x00];

    internal Baron(ObjectTable objects, EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);

        _objects = objects;
        _entities = entities;
    }

    // $A775. How many passes the Baron has still to wait.
    internal Span<byte> Waiting => _pause;

    // $E765. Which way the sprite composer draws him.
    internal Span<byte> Facing => _facing;

    // $0D4E sends a slot here when its type is $2E or $30.
    internal static bool Is(byte type) => type is PlayerOneType or PlayerTwoType;

    // $1473. slot is the ObjectTable slot, and it is also the number of the player the Baron chases.
    internal void Step(int slot)
    {
        if (_entities.State[slot] == Dying || Catch(slot))
        {
            Go(slot);
            return;
        }

        // $14A7. A run is over: the next, and the pause before it.
        if ((_objects.EnemyType[slot] & 0x7F) == 0)
        {
            Plan(slot);
        }

        if (_pause[slot] != 0)
        {
            _pause[slot]--;
            return;
        }

        byte column = Cell(_entities.X[slot], LeftEdge);
        _facing[slot] = column >= _objects.Column[slot] ? FacingRight : FacingLeft;
        byte row = Cell(_entities.Y[slot], TopEdge);

        // $14F7. The run's count is in the low seven bits, and bit 7 picks the axis.
        _objects.EnemyType[slot]--;

        if ((_objects.EnemyType[slot] & 0x80) == 0)
        {
            _objects.Row[slot] = Towards(slot, _objects.Row[slot], row);
        }
        else
        {
            _objects.Column[slot] = Towards(slot, _objects.Column[slot], column);
        }
    }

    private static byte Cell(byte position, byte edge) => (byte)(unchecked((byte)(position - edge)) >> 3);

    // $152B. The `asl`s leave bit 5 of the cell in the carry, and the `adc` adds it.
    private static byte Corner(byte cell, byte origin)
        => unchecked((byte)((cell << 3) + origin + ((cell >> 5) & 0x01)));

    // $14B0 to $14C8. $AA30's low four bits count the runs, but never to a multiple of sixteen, and
    // its top bit flips each time. $AA42 takes the result as the new run: bit 7 picks the axis and the
    // rest is how long it lasts.
    private void Plan(int slot)
    {
        _objects.Variant[slot]++;

        if ((_objects.Variant[slot] & 0x0F) == 0)
        {
            _objects.Variant[slot]--;
        }

        _objects.Variant[slot] ^= 0x80;
        _objects.EnemyType[slot] = _objects.Variant[slot];
        _pause[slot] = Pause;
    }

    // $1500 and $1512. One cell towards the target, and a run that has arrived ends.
    private byte Towards(int slot, byte from, byte to)
    {
        if (from == to)
        {
            _objects.EnemyType[slot] = 0;
            return from;
        }

        return unchecked((byte)(from < to ? from + 1 : from - 1));
    }

    // $152B. Either player, from the second down, and the first caught is the only one.
    private bool Catch(int slot)
    {
        byte x = Corner(_objects.Column[slot], ColumnOrigin);
        byte y = Corner(_objects.Row[slot], RowOrigin);

        for (int player = 1; player >= 0; player--)
        {
            byte state = _entities.State[player];

            if (state is 0 or (not CatchableState and >= CatchLimit))
            {
                continue;
            }

            if (Distance.Absolute(_entities.X[player], x) < CatchRange
                && Distance.Absolute(_entities.Y[player], y) < CatchRange)
            {
                _entities.State[player] = Dying;
                return true;
            }
        }

        return false;
    }

    // $1479. The Baron goes. $1490's store in $2B when the other Baron is gone too is the level
    // timer's, and not here.
    private void Go(int slot)
    {
        _objects.Type[slot] = GoneType;
        _objects.EnemyType[slot] = GoneType;
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Specials;

// `$1319` (`player-sprites.s`, "update_player_sprites"): the fire a fire bubble leaves on a platform. Five strips at most
// (`$A761`, `$A766`, `$A76B`, `$A770`), each a row of cells that burns for a few seconds; enemies standing on it die.
// `FallingFire` makes them where the drop lands.
//
//   * A strip's `Timer` (`$A770`) counts down every fourth pass and is free when negative. It starts at 6. The
//     character drawn is `$ACBD[timer]`: `$1C` at 6 and 2, `$40` at 5 and 3, `$41` at 4, and nothing (the background put
//     back) at 1 and 0. The picture is the timer as it was before this pass's count.
//   * While it burns at its hottest ($40 or more: timers 5, 4 and 3) an enemy in state 1 to `$0A` whose Y is exactly
//     `$10` below the strip's, and whose X is within the strip's length in pixels to its right, is killed (`$1A6F`).
internal sealed class FireStrips
{
    internal const int Capacity = 5;
    internal const byte Free = 0xFF;
    internal const byte Lit = 6;

    private const byte HotFrom = 0x40;
    private const byte KillAbove = 0x0B;
    private const int FirstEnemy = 2;

    private const byte XOrigin = 0x14;
    private const byte YOrigin = 0x15;
    private const byte Standing = 0x10;

    private static readonly byte[] s_characters =
        [0x00, 0x00, 0x1C, 0x40, 0x41, 0x40, 0x1C, 0x00, 0x08, 0x08, 0x09, 0x09, 0x08, 0x08, 0x02, 0x02];

    private readonly EntityTable _entities;
    private readonly FoodDrop _food;

    private readonly byte[] _columns = [0x11, 0x0C, 0x05, 0x05, 0x04];
    private readonly byte[] _rows = [0x1B, 0x0F, 0x0F, 0x0F, 0x08];
    private readonly byte[] _lengths = [0x01, 0x0D, 0x0E, 0x0F, 0x01];
    private readonly byte[] _timers = [Free, Free, Free, Free, Free];

    private readonly byte[] _drawn = new byte[Capacity];

    internal FireStrips(EntityTable entities, FoodDrop food)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(food);

        _entities = entities;
        _food = food;
    }

    // $A761.
    internal Span<byte> Columns => _columns;

    // $A766.
    internal Span<byte> Rows => _rows;

    // $A76B.
    internal Span<byte> Lengths => _lengths;

    // $A770.
    internal Span<byte> Timers => _timers;

    // The character each strip was drawn with in the last pass ($ACBD[timer]), for the rendition. A strip that is free
    // draws nothing.
    internal Span<byte> Characters => _drawn;

    // $11F2-$126C: the strip a landed drop makes, in the slot found.
    internal void Light(int strip, byte column, byte row, byte length)
    {
        _columns[strip] = column;
        _rows[strip] = row;
        _lengths[strip] = length;
        _timers[strip] = Lit;
    }

    // The highest free strip (`$11E7`), or -1.
    internal int FindFree()
    {
        for (int strip = Capacity - 1; strip >= 0; strip--)
        {
            if ((_timers[strip] & 0x80) != 0)
            {
                return strip;
            }
        }

        return -1;
    }

    // $1319. `counter` is `$597F`.
    internal void Update(byte counter)
    {
        for (int strip = Capacity - 1; strip >= 0; strip--)
        {
            byte timer = _timers[strip];

            if ((timer & 0x80) != 0)
            {
                _drawn[strip] = 0;
                continue;
            }

            if ((counter & 0x03) == 0)
            {
                _timers[strip]--;
            }

            byte character = s_characters[timer];

            _drawn[strip] = character;

            if (character >= HotFrom)
            {
                Burn(strip);
            }
        }
    }

    // $1372-$13BC.
    private void Burn(int strip)
    {
        byte x = (byte)((_columns[strip] << 3) + XOrigin);
        byte y = (byte)((_rows[strip] << 3) + YOrigin);
        byte reach = (byte)(_lengths[strip] << 3);

        for (int enemy = EntityTable.Capacity - FirstEnemy - 1; enemy >= 0; enemy--)
        {
            int slot = enemy + FirstEnemy;
            byte state = _entities.State[slot];

            if (state == 0
                || state >= KillAbove
                || (byte)(y - _entities.Y[slot]) != Standing
                || (byte)(_entities.X[slot] - x) >= reach)
            {
                continue;
            }

            _food.Kill(enemy);
            _entities.AttackTimer[slot]--;
        }
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Bubbles;

// $0AAB in collision.s: a player walking into a bubble pushes it along.
//
// The reference heads this "player-enemy collision" and calls its second arm "kill player". Neither is
// what the code does. It walks the eighteen ObjectTable slots, not the enemies, and nothing in it
// touches a player: a player facing right (frame 0 to 3) nudges a bubble on their right four pixels
// right, and one facing left (frame 4 to 7) nudges a bubble on their left four pixels left. A player
// dies at $1D32, the end of $1CBD.
//
// After the nudge, the bubble's cells are read off the map, and a bubble now in a wall is put back one
// column and snapped to the grid.
//
// **The left arm loses the player.** $0B08 parks the player index in $3C and loads the bubble's row
// into Y. A row outside $04 to $1C takes the branch at $0B0E or $0B12 to the loop's `dex`, not to
// $0B51 where Y is put back, so the rest of the walk - and the `dey` that picks the next player - run
// with the row as the player index. That is reproduced: every read is `$B2,y`, `$BA,y`, `$C2,y` or
// `$8520,y`, and Peek finds the table each lands in. Row 3 makes entity 3 the pusher for the rest of
// the slots, and then entities 2, 1 and 0 each get a turn as if they were players. A read that lands
// outside zero page's entity and object arrays, or past the eight frames, throws, by the loud-gap
// rule. The right arm clamps the row instead, and is safe.
internal sealed class BubblePush
{
    // $0AB0. Only a player walking about pushes.
    private const byte PlayingState = 0x01;

    // $0AB7 and $0ABD. Frames 8 upwards push nothing; 4 to 7 face left.
    private const byte NoPushFrame = 0x08;
    private const byte LeftFrame = 0x04;

    // $0ACB and $0B5D. Types $24 upwards are not bubbles a player can move.
    private const byte SpecialType = 0x24;

    // $0AD6, $0B68 and $0AE7/$0B79. One-sided in X, absolute in Y.
    private const byte LeftRange = 0x14;
    private const byte RightRange = 0x12;
    private const byte YRange = 0x0E;

    // $0B0C, $0B10 and $0BA0 to $0BAA. The rows whose map reads stay on the map.
    private const byte TopRow = 0x04;
    private const byte BottomRow = 0x1D;

    // See EnemyDispatcher: an object's row is four more than SolidMap's.
    private const int RowBias = 4;
    private const int Stride = 40;

    // $B2, $BA, $C2, $CA, $DC and $EE in zero page, and $8520: where `base,y` reads land.
    private const int StateBase = 0xB2;
    private const int XBase = 0xBA;
    private const int YBase = 0xC2;
    private const int TypeBase = 0xCA;
    private const int ColumnBase = 0xDC;
    private const int RowBase = 0xEE;
    private const int FrameBase = 0x8520;

    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;

    internal BubblePush(EntityTable entities, ObjectTable objects)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);

        _entities = entities;
        _objects = objects;
    }

    // $0AAB. Y is player 2 first, then player 1; for each, slot 17 down to 0. Y is a byte, and the
    // `dey`/`bpl` at $0AEE stops only when it goes negative.
    internal void Update(SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte y = PlayerTable.Capacity - 1;

        do
        {
            y = Player(y, map);
            y--;
        }
        while (y < 0x80);
    }

    // $0AAD to $0AEC, for whatever Y holds. Returns Y as the walk leaves it.
    private byte Player(byte y, SolidMap map)
    {
        if (Peek(StateBase + y) != PlayingState)
        {
            return y;
        }

        byte frame = Peek(FrameBase + y);

        if (frame >= NoPushFrame)
        {
            return y;
        }

        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            if (_objects.State[slot] != 0 || _objects.Type[slot] >= SpecialType)
            {
                continue;
            }

            if (frame >= LeftFrame)
            {
                y = PushLeft(y, slot, map);
            }
            else
            {
                PushRight(y, slot, map);
            }
        }

        return y;
    }

    // A byte the 6502 reads at `base,y`: zero page's entity and object arrays, and the eight frames.
    private byte Peek(int address) => address switch
    {
        >= StateBase and < XBase => _entities.State[address - StateBase],
        >= XBase and < YBase => _entities.X[address - XBase],
        >= YBase and < TypeBase => _entities.Y[address - YBase],
        >= TypeBase and < ColumnBase => _objects.Type[address - TypeBase],
        >= ColumnBase and < RowBase => _objects.Column[address - ColumnBase],
        >= RowBase and < RowBase + ObjectTable.Capacity => _objects.Row[address - RowBase],
        >= FrameBase and < FrameBase + EntityTable.Capacity => _entities.Frame[address - FrameBase],
        _ => throw new NotSupportedException($"$0AAB reading ${address:X4} with the row in Y is not translated"),
    };

    private bool Near(byte y, int slot, byte dx, byte range)
    {
        if (dx >= range)
        {
            return false;
        }

        // $0ADA to $0AE7. The absolute difference: a borrow leaves the carry clear for the `adc #$01`.
        byte py = Peek(YBase + y);
        byte dy = (byte)(py - _objects.Y[slot]);

        if (py < _objects.Y[slot])
        {
            dy = (byte)(~dy + 1);
        }

        return dy < YRange;
    }

    // $0AC4 to $0B53. Returns Y as the 6502 leaves it: the player, or the row that took its place.
    private byte PushLeft(byte y, int slot, SolidMap map)
    {
        byte x = _objects.X[slot];

        if (!Near(y, slot, (byte)(Peek(XBase + y) - x), LeftRange))
        {
            return y;
        }

        // $0AF2. The carry is clear from the `bcc`, so the `sbc #$03` takes four.
        _objects.X[slot] = (byte)(x - 4);
        int borrow = x >= 4 ? 0 : 1;

        // $0AFA. The `sbc #$02` runs with the carry the X subtraction left.
        int sub = _objects.SubX[slot] - 2 - borrow;

        if (sub < 0)
        {
            sub &= 0x03;
            _objects.Column[slot]--;
        }

        _objects.SubX[slot] = (byte)sub;

        // $0B0A to $0B12. Off the map's rows, and Y keeps the row.
        byte row = _objects.Row[slot];

        if (row is < TopRow or >= BottomRow)
        {
            return row;
        }

        // $0B24 to $0B39. The cell's own column, down three rows; the top only part way down a row.
        if (Walled(slot, map, 0))
        {
            // $0B3B. Back a column, and onto the grid. `sbc #$14` sets the carry the `adc #$07` adds,
            // and that one sets the carry the `adc #$14` adds.
            _objects.Column[slot]++;
            _objects.SubX[slot] = 0;

            int a = (byte)(_objects.X[slot] - 0x14);
            int carry = _objects.X[slot] >= 0x14 ? 1 : 0;
            a += 0x07 + carry;
            carry = a > 0xFF ? 1 : 0;
            _objects.X[slot] = (byte)((a & 0xF8) + 0x14 + carry);
        }

        // $0B51.
        return y;
    }

    // $0B56 to $0BEA.
    private void PushRight(byte y, int slot, SolidMap map)
    {
        byte x = _objects.X[slot];

        if (!Near(y, slot, (byte)(x - Peek(XBase + y)), RightRange))
        {
            return;
        }

        // $0B83. The carry is clear from the `bcc`.
        int sum = x + 4;
        _objects.X[slot] = (byte)sum;

        // $0B8B. The `adc #$02` takes the carry the X addition left; a smaller result is a new column.
        byte old = _objects.SubX[slot];
        byte sub = (byte)((old + 2 + (sum > 0xFF ? 1 : 0)) & 0x03);

        if (sub < old)
        {
            _objects.Column[slot]++;
        }

        _objects.SubX[slot] = sub;

        // $0BA0 to $0BAC. This arm clamps the row, and leaves the carry clear for the pointer sum.
        if (!Walled(slot, map, 1, Math.Clamp(_objects.Row[slot], TopRow, (byte)(BottomRow - 1))))
        {
            return;
        }

        // $0BD4. Back a column, and onto the grid. `and` keeps the carry `sbc #$1C` left.
        _objects.Column[slot]--;
        _objects.SubX[slot] = 0;

        byte pushed = _objects.X[slot];
        int carry = pushed >= 0x1C ? 1 : 0;
        _objects.X[slot] = (byte)((((byte)(pushed - 0x1C)) & 0xF8) + 0x13 + carry);
    }

    private bool Walled(int slot, SolidMap map, int column) => Walled(slot, map, column, _objects.Row[slot]);

    // $0B24 and $0BBD. Rows +0, +1 and +2 from $AD1E's, the first only when SubY is not zero.
    private bool Walled(int slot, SolidMap map, int column, int row)
    {
        // The column is added to the row's address, so one past the forty-byte row is the next row.
        int offset = _objects.Column[slot] + column;
        int mapRow = row - RowBias + (offset / Stride);
        int mapColumn = offset % Stride;

        return (_objects.SubY[slot] != 0 && map[mapRow, mapColumn])
            || map[mapRow + 1, mapColumn]
            || map[mapRow + 2, mapColumn];
    }
}

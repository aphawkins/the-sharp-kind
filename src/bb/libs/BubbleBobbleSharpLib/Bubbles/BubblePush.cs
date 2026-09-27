// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Bubbles;

internal sealed class BubblePush
{
    private const byte PlayingState = 0x01;

    private const byte NoPushFrame = 0x08;
    private const byte LeftFrame = 0x04;

    private const byte SpecialType = 0x24;

    private const byte LeftRange = 0x14;
    private const byte RightRange = 0x12;
    private const byte YRange = 0x0E;

    private const byte TopRow = 0x04;
    private const byte BottomRow = 0x1D;

    private const int RowBias = 4;
    private const int Stride = 40;

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

        byte py = Peek(YBase + y);
        byte dy = (byte)(py - _objects.Y[slot]);

        if (py < _objects.Y[slot])
        {
            dy = (byte)(~dy + 1);
        }

        return dy < YRange;
    }

    private byte PushLeft(byte y, int slot, SolidMap map)
    {
        byte x = _objects.X[slot];

        if (!Near(y, slot, (byte)(Peek(XBase + y) - x), LeftRange))
        {
            return y;
        }

        _objects.X[slot] = (byte)(x - 4);
        int borrow = x >= 4 ? 0 : 1;

        int sub = _objects.SubX[slot] - 2 - borrow;

        if (sub < 0)
        {
            sub &= 0x03;
            _objects.Column[slot]--;
        }

        _objects.SubX[slot] = (byte)sub;

        byte row = _objects.Row[slot];

        if (row is < TopRow or >= BottomRow)
        {
            return row;
        }

        if (Walled(slot, map, 0))
        {
            _objects.Column[slot]++;
            _objects.SubX[slot] = 0;

            int a = (byte)(_objects.X[slot] - 0x14);
            int carry = _objects.X[slot] >= 0x14 ? 1 : 0;
            a += 0x07 + carry;
            carry = a > 0xFF ? 1 : 0;
            _objects.X[slot] = (byte)((a & 0xF8) + 0x14 + carry);
        }

        return y;
    }

    private void PushRight(byte y, int slot, SolidMap map)
    {
        byte x = _objects.X[slot];

        if (!Near(y, slot, (byte)(x - Peek(XBase + y)), RightRange))
        {
            return;
        }

        int sum = x + 4;
        _objects.X[slot] = (byte)sum;

        byte old = _objects.SubX[slot];
        byte sub = (byte)((old + 2 + (sum > 0xFF ? 1 : 0)) & 0x03);

        if (sub < old)
        {
            _objects.Column[slot]++;
        }

        _objects.SubX[slot] = sub;

        if (!Walled(slot, map, 1, Math.Clamp(_objects.Row[slot], TopRow, (byte)(BottomRow - 1))))
        {
            return;
        }

        _objects.Column[slot]--;
        _objects.SubX[slot] = 0;

        byte pushed = _objects.X[slot];
        int carry = pushed >= 0x1C ? 1 : 0;
        _objects.X[slot] = (byte)((((byte)(pushed - 0x1C)) & 0xF8) + 0x13 + carry);
    }

    private bool Walled(int slot, SolidMap map, int column) => Walled(slot, map, column, _objects.Row[slot]);

    private bool Walled(int slot, SolidMap map, int column, int row)
    {
        int offset = _objects.Column[slot] + column;
        int mapRow = row - RowBias + (offset / Stride);
        int mapColumn = offset % Stride;

        return (_objects.SubY[slot] != 0 && map[mapRow, mapColumn])
            || map[mapRow + 1, mapColumn]
            || map[mapRow + 2, mapColumn];
    }
}

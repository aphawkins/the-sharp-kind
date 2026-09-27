// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class Baron
{
    private const byte Dying = 0x0E;

    private const byte GoneType = 0x3A;

    private const byte PlayerOneType = 0x2E;
    private const byte PlayerTwoType = 0x30;

    private const byte CatchableState = 0x18;
    private const byte CatchLimit = 0x0E;

    private const byte CatchRange = 0x10;

    private const byte ColumnOrigin = 0x18;
    private const byte RowOrigin = 0x15;

    private const byte LeftEdge = 0x14;
    private const byte TopEdge = 0x15;

    private const byte Pause = 0x1E;

    private const byte FacingRight = 0x00;
    private const byte FacingLeft = 0x02;

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;

    private readonly byte[] _pause = [0x16, 0x00];
    private readonly byte[] _facing = [0x02, 0x00];

    internal Baron(ObjectTable objects, EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);

        _objects = objects;
        _entities = entities;
    }

    internal Span<byte> Waiting => _pause;

    internal Span<byte> Facing => _facing;

    internal static bool Is(byte type) => type is PlayerOneType or PlayerTwoType;

    internal void Step(int slot)
    {
        if (_entities.State[slot] == Dying || Catch(slot))
        {
            Go(slot);
            return;
        }

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

    private static byte Corner(byte cell, byte origin)
        => unchecked((byte)((cell << 3) + origin + ((cell >> 5) & 0x01)));

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

    private byte Towards(int slot, byte from, byte to)
    {
        if (from == to)
        {
            _objects.EnemyType[slot] = 0;
            return from;
        }

        return unchecked((byte)(from < to ? from + 1 : from - 1));
    }

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

    private void Go(int slot)
    {
        _objects.Type[slot] = GoneType;
        _objects.EnemyType[slot] = GoneType;
    }
}

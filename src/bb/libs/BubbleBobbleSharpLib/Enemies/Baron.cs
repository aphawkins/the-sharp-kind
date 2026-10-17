// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
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
    private readonly LevelTimer _timer;

    private readonly byte[] _pause = [0x16, 0x00];
    private readonly byte[] _facing = [0x02, 0x00];

    internal Baron(ObjectTable objects, EntityTable entities, LevelTimer timer)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(timer);

        _objects = objects;
        _entities = entities;
        _timer = timer;
    }

    internal Span<byte> Waiting => _pause;

    internal Span<byte> Facing => _facing;

    internal static bool Is(byte type) => type is PlayerOneType or PlayerTwoType;

    internal void Step(int slot)
    {
        if (_entities.Slot(slot).State == Dying || Catch(slot))
        {
            Go(slot);
            return;
        }

        if ((_objects.Slot(slot).EnemyType & 0x7F) == 0)
        {
            Plan(slot);
        }

        if (_pause[slot] != 0)
        {
            _pause[slot]--;
            return;
        }

        byte column = Cell(_entities.Slot(slot).X, LeftEdge);
        _facing[slot] = column >= _objects.Slot(slot).Column ? FacingRight : FacingLeft;
        byte row = Cell(_entities.Slot(slot).Y, TopEdge);

        _objects.Slot(slot).EnemyType--;

        if ((_objects.Slot(slot).EnemyType & 0x80) == 0)
        {
            _objects.Slot(slot).Row = Towards(slot, _objects.Slot(slot).Row, row);
        }
        else
        {
            _objects.Slot(slot).Column = Towards(slot, _objects.Slot(slot).Column, column);
        }
    }

    private static byte Cell(byte position, byte edge) => (byte)(unchecked((byte)(position - edge)) >> 3);

    private static byte Corner(byte cell, byte origin)
        => unchecked((byte)((cell << 3) + origin + ((cell >> 5) & 0x01)));

    private void Plan(int slot)
    {
        _objects.Slot(slot).Variant++;

        if ((_objects.Slot(slot).Variant & 0x0F) == 0)
        {
            _objects.Slot(slot).Variant--;
        }

        _objects.Slot(slot).Variant ^= 0x80;
        _objects.Slot(slot).EnemyType = _objects.Slot(slot).Variant;
        _pause[slot] = Pause;
    }

    private byte Towards(int slot, byte from, byte to)
    {
        if (from == to)
        {
            _objects.Slot(slot).EnemyType = 0;
            return from;
        }

        return unchecked((byte)(from < to ? from + 1 : from - 1));
    }

    private bool Catch(int slot)
    {
        byte x = Corner(_objects.Slot(slot).Column, ColumnOrigin);
        byte y = Corner(_objects.Slot(slot).Row, RowOrigin);

        for (int player = 1; player >= 0; player--)
        {
            byte state = _entities.Slot(player).State;

            if (state is 0 or (not CatchableState and >= CatchLimit))
            {
                continue;
            }

            if (Distance.Absolute(_entities.Slot(player).X, x) < CatchRange
                && Distance.Absolute(_entities.Slot(player).Y, y) < CatchRange)
            {
                _entities.Slot(player).State = Dying;
                return true;
            }
        }

        return false;
    }

    // $1479-$1494. When it is the last Baron to go, the clock, which stopped for him, runs again.
    private void Go(int slot)
    {
        _objects.Slot(slot).Type = GoneType;
        _objects.Slot(slot).EnemyType = GoneType;

        if (!Is(_objects.Slot(slot ^ 1).Type))
        {
            _timer.Frames = LevelTimer.FramesPerSecond;
        }
    }
}

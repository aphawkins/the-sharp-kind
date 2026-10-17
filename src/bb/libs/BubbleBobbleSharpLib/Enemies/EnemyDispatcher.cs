// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class EnemyDispatcher
{
    private const int Cell = 0x00;
    private const int CellRight = 0x01;
    private const int Below = 0x28;
    private const int BelowRight = 0x29;
    private const int TwoBelow = 0x50;
    private const int TwoBelowRight = 0x51;

    private const int Stride = 40;

    private const int RowBias = 4;

    private const byte NoProbeType = 0x06;

    private const byte NoRepeatType = 0x0C;

    private const byte MovedType = 0x04;

    private const byte ThunderType = 0x0A;
    private const byte ThunderFlags = 0x0A;

    private const byte CaughtType = 0x34;

    private const byte LastColumn = 0x1C;

    private const byte StepPixels = 0x08;

    private const byte CatchRange = 0x10;

    private const byte GridOrigin = 0x14;
    private const byte GridMask = 0xF8;
    private const byte GridReturn = 0x13;

    private static readonly byte[] s_types = [0x10, 0x04, 0x04, 0x02, 0x02, 0x02, 0x00, 0x00, 0x00];

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;
    private readonly BubbleCollision _collision;
    private readonly RoundState _round;

    internal EnemyDispatcher(ObjectTable objects, EntityTable entities, BubbleCollision collision, RoundState? round = null)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(collision);

        _objects = objects;
        _entities = entities;
        _collision = collision;
        _round = round ?? new();
    }

    internal void Step(int slot, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        Advance(slot, _objects.Slot(slot).State, map);

        if (_objects.Slot(slot).Type >= NoRepeatType)
        {
            return;
        }

        if ((_objects.Slot(slot).EnemyType & 0x80) == 0 || _objects.Slot(slot).State == 0)
        {
            return;
        }

        Advance(slot, _objects.Slot(slot).State, map);
    }

    private void Advance(int slot, byte counter, SolidMap map)
    {
        _objects.Slot(slot).State--;

        int index = counter & 0x7F;

        if ((_objects.Slot(slot).Variant & 0x80) != 0)
        {
            index >>= 1;

            if (index == 0)
            {
                index = 1;
            }
        }

        byte type = s_types[index];
        byte restore = MovedType;

        // $0F73 as `bit`, once the last level has patched it in ($0F76-$0F8D): a bubble with $08 or $09 for its life (the
        // marked player's) is the thunder bubble ($0A) where it would be the plain one, and is left so.
        if (_round.ThunderBlow && _objects.Slot(slot).Flags < ThunderFlags)
        {
            _objects.Slot(slot).Flags++;

            type = type == MovedType ? ThunderType : type;
            restore = ThunderType;
        }

        _objects.Slot(slot).Type = type;

        Move(slot, restore, map);
    }

    private void Move(int slot, byte restore, SolidMap map)
    {
        byte savedColumn = _objects.Slot(slot).Column;
        byte savedX = _objects.Slot(slot).X;

        if ((_objects.Slot(slot).Direction & 0x80) != 0)
        {
            _objects.Slot(slot).Column--;
            _objects.Slot(slot).X -= StepPixels;

            _ = _collision.Check(slot);

            if (_objects.Slot(slot).Type >= NoProbeType)
            {
                return;
            }

            if (Struck(slot, map, Cell, BelowRight, TwoBelow))
            {
                Blocked(slot, restore, savedColumn, savedX);
            }

            return;
        }

        if (savedColumn == LastColumn)
        {
            Blocked(slot, restore, savedColumn, savedX);
            return;
        }

        _objects.Slot(slot).Column++;
        _objects.Slot(slot).X += StepPixels;

        _ = _collision.Check(slot);

        if (_objects.Slot(slot).Type >= NoProbeType)
        {
            return;
        }

        if (Struck(slot, map, CellRight, Below, TwoBelowRight))
        {
            Blocked(slot, restore, savedColumn, savedX);
        }
    }

    private bool Struck(int slot, SolidMap map, int first, int second, int third)
    {
        if (_objects.Slot(slot).SubY != 0 && Solid(slot, map, first))
        {
            return true;
        }

        if (Solid(slot, map, second) || Solid(slot, map, third))
        {
            return true;
        }

        _objects.Slot(slot).State &= 0x7F;
        return false;
    }

    private bool Solid(int slot, SolidMap map, int offset)
        => map[_objects.Slot(slot).Row - RowBias + (offset / Stride), _objects.Slot(slot).Column + (offset % Stride)];

    private void Blocked(int slot, byte restore, byte savedColumn, byte savedX)
    {
        int difference = savedX - GridOrigin;
        _objects.Slot(slot).X = (byte)(((byte)difference & GridMask) + GridReturn + (difference >= 0 ? 1 : 0));

        _objects.Slot(slot).Column = savedColumn;

        byte type = restore;

        if ((_objects.Slot(slot).State & 0x80) != 0)
        {
            _objects.Slot(slot).Direction = Distance.Absolute(_entities.Slot(1).X, _objects.Slot(slot).X) < CatchRange
                && Distance.Absolute(_entities.Slot(1).Y, _objects.Slot(slot).Y) < CatchRange
                ? (byte)1
                : (byte)0;
            _objects.Slot(slot).EnemyType = restore;
            type = CaughtType;
        }

        _objects.Slot(slot).Type = type;

        _objects.Slot(slot).SubX = 0;
        _objects.Slot(slot).State = 0;
    }
}

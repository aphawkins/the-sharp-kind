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

    internal EnemyDispatcher(ObjectTable objects, EntityTable entities, BubbleCollision collision)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(collision);

        _objects = objects;
        _entities = entities;
        _collision = collision;
    }

    internal void Step(int slot, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        Advance(slot, _objects.State[slot], map);

        if (_objects.Type[slot] >= NoRepeatType)
        {
            return;
        }

        if ((_objects.EnemyType[slot] & 0x80) == 0 || _objects.State[slot] == 0)
        {
            return;
        }

        Advance(slot, _objects.State[slot], map);
    }

    private void Advance(int slot, byte counter, SolidMap map)
    {
        _objects.State[slot]--;

        int index = counter & 0x7F;

        if ((_objects.Variant[slot] & 0x80) != 0)
        {
            index >>= 1;

            if (index == 0)
            {
                index = 1;
            }
        }

        _objects.Type[slot] = s_types[index];

        Move(slot, MovedType, map);
    }

    private void Move(int slot, byte restore, SolidMap map)
    {
        byte savedColumn = _objects.Column[slot];
        byte savedX = _objects.X[slot];

        if ((_objects.Direction[slot] & 0x80) != 0)
        {
            _objects.Column[slot]--;
            _objects.X[slot] -= StepPixels;

            _ = _collision.Check(slot);

            if (_objects.Type[slot] >= NoProbeType)
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

        _objects.Column[slot]++;
        _objects.X[slot] += StepPixels;

        _ = _collision.Check(slot);

        if (_objects.Type[slot] >= NoProbeType)
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
        if (_objects.SubY[slot] != 0 && Solid(slot, map, first))
        {
            return true;
        }

        if (Solid(slot, map, second) || Solid(slot, map, third))
        {
            return true;
        }

        _objects.State[slot] &= 0x7F;
        return false;
    }

    private bool Solid(int slot, SolidMap map, int offset)
        => map[_objects.Row[slot] - RowBias + (offset / Stride), _objects.Column[slot] + (offset % Stride)];

    private void Blocked(int slot, byte restore, byte savedColumn, byte savedX)
    {
        int difference = savedX - GridOrigin;
        _objects.X[slot] = (byte)(((byte)difference & GridMask) + GridReturn + (difference >= 0 ? 1 : 0));

        _objects.Column[slot] = savedColumn;

        byte type = restore;

        if ((_objects.State[slot] & 0x80) != 0)
        {
            _objects.Direction[slot] = Distance.Absolute(_entities.X[1], _objects.X[slot]) < CatchRange
                && Distance.Absolute(_entities.Y[1], _objects.Y[slot]) < CatchRange
                ? (byte)1
                : (byte)0;
            _objects.EnemyType[slot] = restore;
            type = CaughtType;
        }

        _objects.Type[slot] = type;

        _objects.SubX[slot] = 0;
        _objects.State[slot] = 0;
    }
}

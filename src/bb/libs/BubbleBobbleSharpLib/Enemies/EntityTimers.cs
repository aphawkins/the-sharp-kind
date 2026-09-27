// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class EntityTimers
{
    private const byte BubbleType = 0x04;

    private const byte WobbleType = 0x48;
    private const byte WobbleToggle = 0x4C;

    private const byte PoppingType = 0x3A;

    private const byte CapturedFrom = 0x18;
    private const byte SpecialFrom = 0x24;

    private const byte FreedType = 0x38;

    private const byte ExpiredType = 0x42;

    private const byte CapturedCountFrom = 0x34;
    private const int CapturedWanted = 0x02;

    private const byte FlickerBelow = 0x11;

    private const int EnemyBase = 2;

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;

    internal EntityTimers(ObjectTable objects, EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);

        _objects = objects;
        _entities = entities;
    }

    internal void Update(byte releasedState)
    {
        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            Tick(slot, releasedState);
        }

        Pressure();
    }

    private void Tick(int slot, byte releasedState)
    {
        _objects.Flags[slot]--;

        if (_objects.Flags[slot] != 0)
        {
            Flicker(slot);
            return;
        }

        byte type = _objects.Type[slot];

        if (type == BubbleType)
        {
            _objects.EnemyType[slot] = BubbleType;
            _objects.Type[slot] = PoppingType;
            return;
        }

        if (type >= SpecialFrom)
        {
            if (type == ExpiredType)
            {
                _objects.Type[slot] = ObjectTable.FreeType;
            }

            return;
        }

        if (type < CapturedFrom)
        {
            return;
        }

        Release(slot, type, releasedState);
    }

    private void Release(int slot, byte type, byte releasedState)
    {
        int entity = ((type - CapturedFrom) >> 1) + EnemyBase;

        _entities.State[entity] = releasedState;
        _entities.Y[entity] |= 0x01;
        _entities.X[entity] &= 0xFE;
        _entities.FlashTimer[entity] = 0xFF;

        _entities.RiseCounter[entity] = _objects.Variant[slot];
        _entities.Frame[entity] = 0;

        _objects.Type[slot] = FreedType;
    }

    private void Flicker(int slot)
    {
        byte timer = _objects.Flags[slot];

        if (timer >= FlickerBelow || (timer & 0x01) != 0)
        {
            return;
        }

        byte type = _objects.Type[slot];

        if (type is BubbleType or WobbleType)
        {
            _objects.Type[slot] = (byte)(type ^ WobbleToggle);
            return;
        }

        if (type is >= CapturedFrom and < SpecialFrom)
        {
            _entities.SpriteEnable ^= (byte)(1 << (((type - CapturedFrom) >> 1) + EnemyBase));
        }
    }

    private void Pressure()
    {
        int captured = 0;

        foreach (byte type in _objects.Type)
        {
            if (type >= CapturedCountFrom)
            {
                captured++;
            }
        }

        if (captured >= CapturedWanted)
        {
            return;
        }

        PopFirst();
        PopFirst();
    }

    private void PopFirst()
    {
        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            if (_objects.Type[slot] != BubbleType)
            {
                continue;
            }

            _objects.EnemyType[slot] = BubbleType;
            _objects.Type[slot] = PoppingType;
            return;
        }
    }
}

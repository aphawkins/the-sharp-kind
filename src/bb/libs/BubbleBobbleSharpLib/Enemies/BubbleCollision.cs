// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class BubbleCollision
{
    private const int EnemyCount = 6;

    private const int EnemyBase = 2;

    private const byte Range = 0x10;

    private const byte SkipFrom = 0x0B;
    private const byte SkipBefore = 0x16;

    private const byte CapturedBase = 0x18;

    private const byte CapturedFlags = 0xA0;

    // $AB81-$AB92, read at $10BD with the class the enemy had. The 6502 would read on into $AB93's loader text for a class
    // above 17, and none gets there: the class is a state of 1 to 9, or, for an enemy released from a bubble ($0A) or
    // tumbling in a bonus round ($16, $17), what it was kept in RiseCounter, which only ever holds such a state
    // (`BonusRound.Start` keeps states below $0B only, `EntityTimers` hands back a bubble's own class, `FoodDrop` rolls 1 to 8).
    private static readonly byte[] s_caughtFrames =
    [
        0x02, 0x00, 0x0B, 0x0B, 0x0B, 0x07, 0x0B, 0x07,
        0x07, 0x05, 0x07, 0x07, 0x07, 0x03, 0x07, 0x03, 0x03, 0x01,
    ];

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;

    internal BubbleCollision(ObjectTable objects, EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);

        _objects = objects;
        _entities = entities;
    }

    internal bool Check(int slot)
    {
        for (int enemy = EnemyCount - 1; enemy >= 0; enemy--)
        {
            int entity = enemy + EnemyBase;
            byte state = _entities.Slot(entity).State;

            if (state is 0 or (>= SkipFrom and < SkipBefore))
            {
                continue;
            }

            if (Distance.Absolute(_entities.Slot(entity).X, _objects.Slot(slot).X) >= Range
                || Distance.Absolute(_entities.Slot(entity).Y, _objects.Slot(slot).Y) >= Range)
            {
                continue;
            }

            Capture(slot, enemy, entity);
            return true;
        }

        return false;
    }

    private void Capture(int slot, int enemy, int entity)
    {
        _objects.Slot(slot).Type = (byte)((enemy << 1) + CapturedBase);

        byte state = _entities.Slot(entity).State;
        byte carried = state;

        if (state is 0x0A or >= SkipBefore)
        {
            carried = _entities.Slot(entity).RiseCounter;

            _entities.Slot(entity).RiseCounter = 0xFF;
            _entities.Slot(entity).FallCounter = 0xFF;
            _entities.Slot(entity).GroundState = 0xFF;
        }

        _objects.Slot(slot).Variant = carried;

        _entities.Slot(entity).State = 0;
        _entities.Slot(entity).HoldTimer = 0;
        _objects.Slot(slot).State = 0;
        _entities.Slot(entity).Mode = 0xFF;
        _objects.Slot(slot).Flags = CapturedFlags;

        _objects.Slot(slot).EnemyType = s_caughtFrames[carried];
    }
}

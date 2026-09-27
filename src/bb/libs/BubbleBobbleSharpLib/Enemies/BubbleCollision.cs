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
            byte state = _entities.State[entity];

            if (state is 0 or (>= SkipFrom and < SkipBefore))
            {
                continue;
            }

            if (Distance.Absolute(_entities.X[entity], _objects.X[slot]) >= Range
                || Distance.Absolute(_entities.Y[entity], _objects.Y[slot]) >= Range)
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
        _objects.Type[slot] = (byte)((enemy << 1) + CapturedBase);

        byte state = _entities.State[entity];
        byte carried = state;

        if (state is 0x0A or >= SkipBefore)
        {
            carried = _entities.RiseCounter[entity];

            _entities.RiseCounter[entity] = 0xFF;
            _entities.FallCounter[entity] = 0xFF;
            _entities.GroundState[entity] = 0xFF;
        }

        _objects.Variant[slot] = carried;

        _entities.State[entity] = 0;
        _entities.HoldTimer[entity] = 0;
        _objects.State[slot] = 0;
        _entities.Mode[entity] = 0xFF;
        _objects.Flags[slot] = CapturedFlags;

        _objects.EnemyType[slot] = s_caughtFrames[carried];
    }
}

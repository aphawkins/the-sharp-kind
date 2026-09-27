// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class EnemyAiLoop
{
    private const byte SpecialType = 0x24;

    private const byte CaughtType = 0x34;

    private const byte PlayerDying = 0x0E;
    private const byte PlayerDead = 0x0F;

    private const byte CatchRange = 0x10;

    private const byte PlayerXNudge = 0x02;

    private const byte MapDraw = 0xEA;

    private const byte NeighbourRange = 0x10;

    private const byte NearEdge = 0x04;
    private const byte FarRow = 0x1C;
    private const byte NearColumn = 0x02;
    private const byte FarColumn = 0x1C;

    private const byte TopRow = 0x04;
    private const byte BottomRow = 0x1C;

    private const int RowBias = 4;

    private const int MapOffset = 0x29;
    private const int Stride = 40;

    private const int OddLevel = 0x48;
    private const byte OddThreshold = 0x02;

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;
    private readonly EnemyDispatcher _dispatcher;
    private readonly EntityMover _mover;
    private readonly BbRandom _random;
    private readonly Baron _baron;

    internal EnemyAiLoop(
        ObjectTable objects,
        EntityTable entities,
        EnemyDispatcher dispatcher,
        EntityMover mover,
        BbRandom random,
        Baron baron)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(mover);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(baron);

        _objects = objects;
        _entities = entities;
        _dispatcher = dispatcher;
        _mover = mover;
        _random = random;
        _baron = baron;
    }

    internal void Update(SolidMap map)
    {
        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            Step(slot, map);
        }
    }

    private void Step(int slot, SolidMap map)
    {
        byte type = _objects.Type[slot];

        if (type >= SpecialType)
        {
            if (Baron.Is(type))
            {
                _baron.Step(slot);
            }

            return;
        }

        if (_objects.State[slot] != 0)
        {
            _dispatcher.Step(slot, map);
            return;
        }

        if (!Caught(slot))
        {
            Wander(slot, map);
        }
    }

    private void Wander(int slot, SolidMap map)
    {
        if (_random.Next() < MapDraw && Neighboured(slot))
        {
            _mover.Step(slot, RandomDirection(slot, map));
            return;
        }

        _mover.Step(slot, MapDirection(slot, map));
    }

    private bool Neighboured(int slot)
    {
        for (int other = ObjectTable.Capacity - 1; other >= 0; other--)
        {
            if (other == slot || _objects.State[other] != 0 || _objects.Type[other] >= SpecialType)
            {
                continue;
            }

            if (Distance.Absolute(_objects.X[slot], _objects.X[other]) < NeighbourRange
                && Distance.Absolute(_objects.Y[slot], _objects.Y[other]) < NeighbourRange)
            {
                return true;
            }
        }

        return false;
    }

    private int RandomDirection(int slot, SolidMap map)
    {
        int level = map.Number - 1;
        byte threshold = level == OddLevel ? OddThreshold : (byte)level;

        return _random.Next() >= threshold ? Sideways(slot) : Vertical(slot);
    }

    private int Sideways(int slot) => _objects.Column[slot] switch
    {
        < NearColumn => EntityMover.Right,
        >= FarColumn => EntityMover.Left,
        _ => _random.Next() | EntityMover.Right,
    };

    private int Vertical(int slot) => _objects.Row[slot] switch
    {
        < NearEdge => EntityMover.Down,
        >= FarRow => EntityMover.Up,
        _ => _random.Next() & EntityMover.Down,
    };

    private int MapDirection(int slot, SolidMap map)
    {
        int row = Math.Clamp((int)_objects.Row[slot], TopRow, BottomRow);

        return map.Direction(row - RowBias + (MapOffset / Stride), _objects.Column[slot] + (MapOffset % Stride));
    }

    private bool Caught(int slot)
    {
        for (int player = 1; player >= 0; player--)
        {
            if (!InRange(slot, player))
            {
                continue;
            }

            _objects.Direction[slot] = (byte)player;
            _objects.EnemyType[slot] = _objects.Type[slot];
            _objects.Type[slot] = CaughtType;
            return true;
        }

        return false;
    }

    private bool InRange(int slot, int player)
        => _entities.State[player] is not (0 or PlayerDying or PlayerDead)
            && Distance.Absolute((byte)(_entities.X[player] + PlayerXNudge), _objects.X[slot]) < CatchRange
            && Distance.Absolute(_entities.Y[player], _objects.Y[slot]) < CatchRange;
}

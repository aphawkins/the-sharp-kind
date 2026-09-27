// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class EnemyShot
{
    private const int ObjectBase = 2;

    private const int ShotSlots = 16;

    private const byte BookedType = 0x42;
    private const byte BookedFlags = 0x09;

    private const byte Countdown = 0x07;

    private const byte Reload = 0x96;

    private const byte RightLimit = 0xF4;
    private const byte LeftLimit = 0x34;

    private const byte ChanceMask = 0x03;
    private const byte OffRowMask = 0x1F;

    private const byte LeftEdge = 0x14;
    private const byte TopEdge = 0x15;

    private const int LeftBeside = 0x27;
    private const int RightBeside = 0x2B;
    private const byte LeftStep = 0xFE;
    private const byte RightStep = 0x02;

    private const byte LeftDirection = 0x80;
    private const byte RightDirection = 0x02;

    private const byte ClassSixState = 0x08;
    private const byte ClassFiveState = 0x07;
    private const byte ClassSixType = 0x2A;
    private const byte ClassFiveType = 0x2C;
    private const byte ShotType = 0x28;
    private const byte ClassFivePause = 0x2C;

    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly BbRandom _random;

    internal EnemyShot(EntityTable entities, ObjectTable objects, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _objects = objects;
        _random = random;
    }

    internal bool Booked(int slot) => (_entities.BubbleTimer[slot] & 0x80) == 0;

    internal void Aim(int slot, int target)
    {
        if (Booked(slot))
        {
            return;
        }

        if (_entities.AttackTimer[slot] != 0)
        {
            _entities.AttackTimer[slot]--;

            if (_entities.AttackTimer[slot] != 0)
            {
                return;
            }
        }

        if ((_entities.RiseCounter[slot] & 0x80) == 0 || (_entities.GroundState[slot] & 0x80) == 0)
        {
            return;
        }

        if ((_random.Next() & ChanceMask) != 0)
        {
            return;
        }

        if (!Facing(slot, target) || !InReach(slot, target))
        {
            return;
        }

        Book(slot);
    }

    internal void Fire(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        _entities.BubbleTimer[slot]--;

        if (_entities.BubbleTimer[slot] != 0)
        {
            return;
        }

        byte booked = _entities.RiseCounter[slot];

        if ((booked & 0x80) != 0)
        {
            _entities.BubbleTimer[slot]++;
            return;
        }

        _entities.RiseCounter[slot] = 0xFF;

        int shot = booked + ObjectBase;
        bool left = _entities.Frame[slot] >= _entities.FrameCount[slot];
        int beside = left ? LeftBeside : RightBeside;

        _objects.Direction[shot] = left ? LeftDirection : RightDirection;

        if (cell.Solid(map, beside) || cell.Solid(map, beside + 1))
        {
            _objects.Type[shot] = ObjectTable.FreeType;
            return;
        }

        byte column = unchecked((byte)(Cell(_entities.X[slot], LeftEdge) + (left ? LeftStep : RightStep)));
        _objects.Column[shot] = column;
        _objects.X[shot] = column;
        _objects.Row[shot] = Cell(_entities.Y[slot], TopEdge);
        _objects.Y[shot] = (byte)slot;
        _objects.SubX[shot] = 0;
        _objects.SubY[shot] = 0;
        _objects.Type[shot] = Load(slot, shot, booked);
    }

    private static byte Cell(byte position, byte edge) => (byte)(unchecked((byte)(position - edge)) >> 3);

    private bool Facing(int slot, int target)
    {
        byte self = _entities.X[slot];
        bool left = _entities.Frame[slot] >= _entities.FrameCount[slot];

        bool rightOfPlayer = self >= _entities.X[target] + (_random.Carry ? 0 : 1);

        return rightOfPlayer ? left && self >= LeftLimit : !left && self < RightLimit;
    }

    private bool InReach(int slot, int target)
    {
        byte self = _entities.Y[slot];
        byte player = _entities.Y[target];

        return self >= player && (self == player || (_random.Next() & OffRowMask) == 0);
    }

    private void Book(int slot)
    {
        for (int shot = ShotSlots - 1; shot >= 0; shot--)
        {
            if ((_objects.Type[shot + ObjectBase] & 0x80) == 0)
            {
                continue;
            }

            _objects.Type[shot + ObjectBase] = BookedType;
            _entities.RiseCounter[slot] = (byte)shot;
            _entities.BubbleTimer[slot] = Countdown;
            _entities.AttackTimer[slot] = Reload;
            _objects.Flags[shot + ObjectBase] = BookedFlags;
            return;
        }
    }

    private byte Load(int slot, int shot, byte booked)
    {
        switch (_entities.State[slot])
        {
            case ClassSixState:
                return ClassSixType;
            case ClassFiveState:
                _entities.AttackTimer[slot] = booked;
                _entities.BubbleTimer[slot] = ClassFivePause;
                return ClassFiveType;
            default:
                _objects.SubX[shot] = (byte)(_objects.Direction[shot] & 0x7F);
                return ShotType;
        }
    }
}

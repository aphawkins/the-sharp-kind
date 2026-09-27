// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class EnemyRunner
{
    private const int ObjectBase = 2;

    private const int ThrowSlots = 16;

    private const byte ThrowChance = 0x0C;

    private const byte LowestThrow = 0xA8;

    private const byte ThrownType = 0x32;

    private const byte ThrowXOffset = 0x0C;
    private const byte ThrowYOffset = 0x05;

    private const byte Reload = 0x64;

    private const int FloorLeft = 0x51;
    private const int FloorMiddle = 0x52;
    private const int FloorRight = 0x53;

    private const byte LeftBit = 0x01;
    private const byte RightBit = 0x02;

    private const int LeftBeside = 0x28;
    private const int RightBeside = 0x2B;
    private const byte StepLeft = 0xFC;
    private const byte StepRight = 0x04;

    private const byte KeptBits = 0x04;

    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly PlayerDescent _descent;
    private readonly BbRandom _random;

    internal EnemyRunner(EntityTable entities, ObjectTable objects, PlayerDescent descent, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(descent);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _objects = objects;
        _descent = descent;
        _random = random;
    }

    internal void Step(int slot, int target, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        Throw(slot);

        byte heading = _entities.Heading[slot];

        if ((_entities.GroundState[slot] & 0x80) == 0)
        {
            Descend(slot, target, cell, map);
            return;
        }

        if (!EnemyProbes.Blocked(cell, map, FloorLeft, FloorMiddle, FloorRight))
        {
            _entities.GroundState[slot]++;
            Descend(slot, target, cell, map);
            return;
        }

        if ((heading & LeftBit) != 0)
        {
            EnemyProbes.Sidestep(_entities, slot, cell, map, LeftBeside, StepLeft);
        }

        if ((heading & RightBit) != 0)
        {
            EnemyProbes.Sidestep(_entities, slot, cell, map, RightBeside, StepRight);
        }

        EntityAnimation.Toggle(_entities, slot);
    }

    private void Throw(int slot)
    {
        if (_entities.AttackTimer[slot] != 0)
        {
            _entities.AttackTimer[slot]--;

            if (_entities.AttackTimer[slot] != 0)
            {
                return;
            }
        }

        if (_random.Next() >= ThrowChance
            || (_entities.GroundState[slot] & 0x80) == 0
            || (_entities.BubbleTimer[slot] & 0x80) == 0
            || _entities.Y[slot] >= LowestThrow)
        {
            return;
        }

        for (int i = ThrowSlots - 1; i >= 0; i--)
        {
            int thrown = i + ObjectBase;

            if ((_objects.Type[thrown] & 0x80) == 0)
            {
                continue;
            }

            _objects.Type[thrown] = ThrownType;
            _objects.EnemyType[thrown] = ThrownType;
            _objects.X[thrown] = (byte)slot;
            _objects.Column[thrown] = (byte)(unchecked((byte)(_entities.X[slot] - ThrowXOffset)) >> 3);
            _objects.Row[thrown] = (byte)(unchecked((byte)(_entities.Y[slot] - ThrowYOffset)) >> 3);
            _entities.AttackTimer[slot] = Reload;
            _entities.BubbleTimer[slot] = Reload;
            return;
        }
    }

    private void Descend(int slot, int target, in PlayerCell cell, SolidMap map)
    {
        byte ground = _entities.GroundState[slot];

        _descent.Step(slot, cell, map);

        if (_entities.GroundState[slot] != ground)
        {
            byte towards = _entities.X[slot] >= _entities.X[target] ? LeftBit : RightBit;
            _entities.Heading[slot] = (byte)(towards | (_entities.Heading[slot] & KeptBits));
        }

        EntityAnimation.Toggle(_entities, slot);
    }
}

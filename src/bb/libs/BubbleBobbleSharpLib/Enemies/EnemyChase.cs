// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class EnemyChase
{
    private const byte ChaseMask = 0x0B;

    private readonly EntityTable _entities;

    internal EnemyChase(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities;
    }

    internal void Step(int slot, int target)
    {
        byte player = _entities.Slot(target).Y;
        byte self = _entities.Slot(slot).Y;

        if (player < self)
        {
            Alternate(slot);
            return;
        }

        _entities.Slot(slot).LeapFlag = player == self ? (byte)0xFF : (byte)0x00;
        _entities.Slot(slot).ClimbFlag = 0;
        _entities.Slot(slot).Heading &= ChaseMask;
    }

    private void Alternate(int slot)
    {
        _entities.Slot(slot).TurnTimer--;

        if (_entities.Slot(slot).TurnTimer != 0)
        {
            return;
        }

        bool climb = _entities.Slot(slot).ClimbNext;

        _entities.Slot(slot).LeapFlag = climb ? (byte)0x00 : (byte)0xFF;
        _entities.Slot(slot).ClimbFlag = climb ? (byte)0xFF : (byte)0x00;
        _entities.Slot(slot).ClimbNext = !climb;
        _entities.Slot(slot).TurnTimer = _entities.Slot(slot).TurnInterval;
    }
}

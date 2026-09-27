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
        byte player = _entities.Y[target];
        byte self = _entities.Y[slot];

        if (player < self)
        {
            Alternate(slot);
            return;
        }

        _entities.LeapFlag[slot] = player == self ? (byte)0xFF : (byte)0x00;
        _entities.ClimbFlag[slot] = 0;
        _entities.Heading[slot] &= ChaseMask;
    }

    private void Alternate(int slot)
    {
        _entities.TurnTimer[slot]--;

        if (_entities.TurnTimer[slot] != 0)
        {
            return;
        }

        bool climb = _entities.ClimbNext[slot];

        _entities.LeapFlag[slot] = climb ? (byte)0x00 : (byte)0xFF;
        _entities.ClimbFlag[slot] = climb ? (byte)0xFF : (byte)0x00;
        _entities.ClimbNext[slot] = !climb;
        _entities.TurnTimer[slot] = _entities.TurnInterval[slot];
    }
}

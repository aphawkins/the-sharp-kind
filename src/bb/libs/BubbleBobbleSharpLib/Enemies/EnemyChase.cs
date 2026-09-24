// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $EE62 in entity-system.s: what an enemy may try, from where its player is.
//
// The walker at $EA08 and the hopper at $1F2E both call it first. The player's row against the
// enemy's decides it: level, and the enemy may leap; below, and it may do nothing; above, and the leap
// and the climb take turns, each for as long as the turn timer runs.
internal sealed class EnemyChase
{
    // $EE70 and $EE8A. $EE62 keeps bits 0, 1 and 3 of the heading and clears bit 2, the jump up.
    private const byte ChaseMask = 0x0B;

    private readonly EntityTable _entities;

    internal EnemyChase(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities;
    }

    // $EE62. target is the slot $1CF3 put in $4B.
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

    // $EE91. The player is above. Each time the turn timer runs out, the leap and the climb swap.
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

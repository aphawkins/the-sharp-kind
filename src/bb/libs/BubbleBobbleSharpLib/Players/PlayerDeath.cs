// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerDeath
{
    private const byte PlayingState = 0x01;

    private const byte DeadlyStates = 0x0B;

    private const byte Range = 0x0C;

    private const byte DyingState = 0x0E;

    private const int FirstEnemy = 2;

    private readonly EntityTable _entities;

    internal PlayerDeath(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities;
    }

    internal void Update()
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (_entities.State[player] != PlayingState)
            {
                continue;
            }

            int x = _entities.X[player] + 1 + 1;
            byte y = (byte)(_entities.Y[player] + 2 + (x > 0xFF ? 1 : 0));

            for (int enemy = EntityTable.Capacity - 1; enemy >= FirstEnemy; enemy--)
            {
                byte state = _entities.State[enemy];

                if (state is 0 or >= DeadlyStates)
                {
                    continue;
                }

                if (Distance(_entities.X[enemy], (byte)x) < Range && Distance(_entities.Y[enemy], y) < Range)
                {
                    _entities.State[player] = DyingState;
                }
            }
        }
    }

    private static byte Distance(byte from, byte to)
        => from >= to ? (byte)(from - to) : (byte)(to - from);
}

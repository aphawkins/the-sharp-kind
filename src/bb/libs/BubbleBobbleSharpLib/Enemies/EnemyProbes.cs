// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal static class EnemyProbes
{
    private const int ClimbRows = 4;

    private static readonly int[] s_climbColumn = [0x02, 0x2A, 0x52, 0x7A, 0xA2];

    internal static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left) || cell.Solid(map, middle) || (cell.FineX != 0 && cell.Solid(map, right));

    internal static bool PlatformAbove(in PlayerCell cell, SolidMap map)
    {
        PlayerCell top = cell with { Row = cell.Row - ClimbRows };

        for (int i = 0; i < ClimbRows; i++)
        {
            if (!top.Solid(map, s_climbColumn[i]) && top.Solid(map, s_climbColumn[i + 1]))
            {
                return true;
            }
        }

        return false;
    }

    internal static void Sidestep(EntityTable entities, int slot, in PlayerCell cell, SolidMap map, int beside, byte step)
    {
        ArgumentNullException.ThrowIfNull(entities);

        if (cell.FineX == 0 && cell.Solid(map, beside))
        {
            entities.Heading[slot] ^= 0x03;
            return;
        }

        entities.X[slot] = unchecked((byte)(entities.X[slot] + step));
    }
}

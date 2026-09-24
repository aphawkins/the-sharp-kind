// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// The map reads and the side step that more than one enemy handler makes in the same words.
internal static class EnemyProbes
{
    // $EA20 and $1F66. Four rows above the thing, where the column below starts.
    private const int ClimbRows = 4;

    // $EA20 and $1F66. Five cells of one column, from four rows above the thing down to its own row.
    private static readonly int[] s_climbColumn = [0x02, 0x2A, 0x52, 0x7A, 0xA2];

    // Three cells of a row, the third only when the thing straddles a column.
    internal static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left) || cell.Solid(map, middle) || (cell.FineX != 0 && cell.Solid(map, right));

    // $EA20 to $EA62, and $1F66 to $1FA1. Is there a platform above to jump onto: in the column above
    // the thing, a solid cell with an open one above it, within four rows.
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

    // $ECF7. Off a column boundary the thing moves without looking. On one, a wall beside it turns
    // it round by flipping both walk bits of its heading.
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

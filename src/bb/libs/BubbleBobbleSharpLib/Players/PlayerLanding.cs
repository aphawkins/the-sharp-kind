// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerLanding
{
    private const byte ColumnMask = 0xFE;

    private const int ClearRowLeft = 0x29;
    private const int ClearRowMiddle = 0x2A;
    private const int ClearRowRight = 0x2B;
    private const int FloorRowLeft = 0x51;
    private const int FloorRowMiddle = 0x52;
    private const int FloorRowRight = 0x53;

    private const int SpecialLevel = 0x5B;

    private const byte SpecialHeight = 0xDD;

    private readonly EntityTable _entities;
    private readonly PlayerDescent _descent;

    internal PlayerLanding(EntityTable entities, PlayerDescent descent)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(descent);

        _entities = entities;
        _descent = descent;
    }

    internal void Step(int player, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        _entities.RiseCounter[player] = 0xFF;
        _entities.FallCounter[player] = 0xFF;

        Square(player);

        bool standing = !Blocked(cell, map, ClearRowLeft, ClearRowMiddle, ClearRowRight)
            && Blocked(cell, map, FloorRowLeft, FloorRowMiddle, FloorRowRight);

        if (standing)
        {
            return;
        }

        if (map.Number - 1 == SpecialLevel && _entities.Y[player] == SpecialHeight)
        {
            return;
        }

        _entities.GroundState[player]++;
        _descent.Step(player, cell, map);
    }

    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left)
            || cell.Solid(map, middle)
            || (cell.FineX != 0 && cell.Solid(map, right));

    private void Square(int player)
    {
        byte x = _entities.X[player];

        _entities.X[player] = _entities.Frame[player] is >= 0x04 and < 0x0A
            ? (byte)(x & ColumnMask)
            : unchecked((byte)((x + 1) & ColumnMask));
    }
}

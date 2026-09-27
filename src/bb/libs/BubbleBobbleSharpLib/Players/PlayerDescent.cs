// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerDescent
{
    private const byte Drop = 0x02;

    private const byte BottomWrap = 0xF5;
    private const byte TopRow = 0x15;

    private const byte GroundCheckFloor = 0x1F;

    private const byte GridBase = 0x2D;

    private const int ClearRowLeft = 0x51;
    private const int ClearRowMiddle = 0x52;
    private const int ClearRowRight = 0x53;
    private const int FloorRowLeft = 0x79;
    private const int FloorRowMiddle = 0x7A;
    private const int FloorRowRight = 0x7B;

    private readonly EntityTable _entities;

    internal PlayerDescent(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities;
    }

    internal void Step(int player, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte y = unchecked((byte)(_entities.Y[player] + Drop));
        _entities.Y[player] = y;

        if (y == BottomWrap)
        {
            _entities.Y[player] = TopRow;
            return;
        }

        if (y < GroundCheckFloor)
        {
            return;
        }

        if ((unchecked((byte)(y - GridBase)) & 0x07) != 0)
        {
            return;
        }

        if (Blocked(cell, map, ClearRowLeft, ClearRowMiddle, ClearRowRight))
        {
            return;
        }

        if (!Blocked(cell, map, FloorRowLeft, FloorRowMiddle, FloorRowRight))
        {
            return;
        }

        _entities.GroundState[player]--;
    }

    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left)
            || cell.Solid(map, middle)
            || (cell.FineX != 0 && cell.Solid(map, right));
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerFall
{
    private const byte Drop = 0x02;

    private const byte BottomWrap = 0xF5;
    private const byte TopRow = 0x15;

    private const byte GroundCheckFloor = 0x26;

    private const byte GridBase = 0x2D;

    private const int ClearRowLeft = 0x51;
    private const int ClearRowMiddle = 0x52;
    private const int ClearRowRight = 0x53;
    private const int FloorRowLeft = 0x79;
    private const int FloorRowMiddle = 0x7A;
    private const int FloorRowRight = 0x7B;

    private const byte ColumnMask = 0xFE;

    private const byte AnimationPeriod = 0x02;

    private readonly EntityTable _entities;
    private readonly PlayerSteer _steer;

    internal PlayerFall(EntityTable entities, PlayerSteer steer)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(steer);

        _entities = entities;
        _steer = steer;
    }

    internal void Step(int player, byte port, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte y = unchecked((byte)(_entities.Y[player] + Drop));
        _entities.Y[player] = y;

        if (y == BottomWrap)
        {
            _entities.Y[player] = TopRow;
            return;
        }

        if (Landed(y, cell, map))
        {
            Land(player);
            return;
        }

        Animate(player, port, map);
    }

    private static bool Landed(byte y, in PlayerCell cell, SolidMap map)
        => y >= GroundCheckFloor
            && (unchecked((byte)(y - GridBase)) & 0x07) == 0
            && !Blocked(cell, map, ClearRowLeft, ClearRowMiddle, ClearRowRight)
            && Blocked(cell, map, FloorRowLeft, FloorRowMiddle, FloorRowRight);

    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left)
            || cell.Solid(map, middle)
            || (cell.FineX != 0 && cell.Solid(map, right));

    private void Land(int player)
    {
        _entities.GroundState[player]--;

        byte x = _entities.X[player];
        byte frame = _entities.Frame[player];

        _entities.X[player] = frame is (>= 0x04 and < 0x08) or >= 0x0A
            ? (byte)(x & ColumnMask)
            : unchecked((byte)((x + 1) & ColumnMask));
    }

    private void Animate(int player, byte port, SolidMap map)
    {
        _entities.AnimationTimer[player]++;

        if (_entities.AnimationTimer[player] < AnimationPeriod)
        {
            return;
        }

        _entities.AnimationTimer[player] = 0;
        _steer.Step(player, port, map);
    }
}

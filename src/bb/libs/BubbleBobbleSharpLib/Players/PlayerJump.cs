// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerJump
{
    private const byte FallEnd = 0x10;

    private const byte TopWrap = 0x05;
    private const byte BottomWrap = 0xF5;
    private const byte WrapStep = 0xF0;

    private const byte GridBase = 0x2D;

    private const byte LandCheckFloor = 0x1F;

    private const int ClearRowLeft = 0x29;
    private const int ClearRowMiddle = 0x2A;
    private const int ClearRowRight = 0x2B;
    private const int FloorRowLeft = 0x51;
    private const int FloorRowMiddle = 0x52;
    private const int FloorRowRight = 0x53;

    private static readonly byte[] s_arc =
    [
        0x00, 0x01, 0x01, 0x02, 0x02, 0x02, 0x03, 0x03,
        0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x04, 0x04,
    ];

    private readonly EntityTable _entities;
    private readonly PlayerDrift _drift;
    private readonly PlayerLanding _landing;

    internal PlayerJump(EntityTable entities, PlayerDrift drift, PlayerLanding landing)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(drift);
        ArgumentNullException.ThrowIfNull(landing);

        _entities = entities;
        _drift = drift;
        _landing = landing;
    }

    internal void Step(int player, byte port, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte rise = _entities.RiseCounter[player];

        if (rise != 0)
        {
            Rise(player, port, map, rise);
            return;
        }

        Fall(player, port, cell, map);
    }

    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left)
            || cell.Solid(map, middle)
            || (cell.FineX != 0 && cell.Solid(map, right));

    private void Rise(int player, byte port, SolidMap map, byte rise)
    {
        _entities.RiseCounter[player] = unchecked((byte)(rise - 1));

        byte y = unchecked((byte)(_entities.Y[player] - s_arc[rise]));

        if (y < TopWrap)
        {
            y = unchecked((byte)(y + WrapStep));
        }

        _entities.Y[player] = y;

        _drift.Enter(player, port, PlayerCell.Of(_entities.X[player], y), map);
    }

    private void Fall(int player, byte port, in PlayerCell cell, SolidMap map)
    {
        byte fall = _entities.FallCounter[player];

        if ((fall & 0x80) != 0)
        {
            Settle(player);
            fall = 0x00;
        }
        else if (fall == FallEnd)
        {
            _landing.Step(player, cell, map);
            return;
        }

        _entities.FallCounter[player] = unchecked((byte)(fall + 1));

        byte y = unchecked((byte)(_entities.Y[player] + s_arc[fall]));

        if (y >= BottomWrap)
        {
            y = unchecked((byte)(y - WrapStep));
        }

        _entities.Y[player] = y;

        if (Land(player, cell, map, y, out PlayerCell landed))
        {
            _landing.Step(player, landed, map);
            return;
        }

        _drift.Step(player, port, PlayerCell.Of(_entities.X[player], y), map);
    }

    private void Settle(int player)
    {
        byte frame = _entities.Frame[player];

        if (frame is (>= 0x02 and < 0x04) or (>= 0x06 and < 0x08))
        {
            _entities.Frame[player] = (byte)(frame & 0x05);
        }

        _entities.FallCounter[player] = 0x00;
    }

    private bool Land(int player, in PlayerCell cell, SolidMap map, byte y, out PlayerCell landed)
    {
        landed = default;

        if (y < LandCheckFloor)
        {
            return false;
        }

        if ((unchecked((byte)(y - GridBase)) & 0x07) >= cell.FineY)
        {
            return false;
        }

        landed = PlayerCell.Of(_entities.X[player], y);

        if (Blocked(landed, map, ClearRowLeft, ClearRowMiddle, ClearRowRight))
        {
            return false;
        }

        if (!Blocked(landed, map, FloorRowLeft, FloorRowMiddle, FloorRowRight))
        {
            return false;
        }

        _entities.Y[player] = unchecked((byte)((unchecked((byte)(y - GridBase)) & 0xF8) + GridBase));
        return true;
    }
}

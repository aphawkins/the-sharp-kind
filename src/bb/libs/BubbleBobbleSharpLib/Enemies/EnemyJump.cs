// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class EnemyJump
{
    private const byte WindUp = 0x1F;
    private const byte RiseStart = 0x10;

    private const byte FallEnd = 0x10;

    private const byte LandCheckFloor = 0x1F;

    private const byte TopEdge = 0x15;
    private const byte GridBase = 0x2D;

    private const byte RightEdge = 0xF4;

    private const byte AcrossStep = 0x02;

    private const int ClearRowLeft = 0x51;
    private const int ClearRowMiddle = 0x52;
    private const int ClearRowRight = 0x53;
    private const int FloorRowLeft = 0x79;
    private const int FloorRowMiddle = 0x7A;
    private const int FloorRowRight = 0x7B;

    private const int LeftMiddle = 0x28;
    private const int LeftBottom = 0x50;
    private const int RightMiddle = 0x2B;
    private const int RightBottom = 0x53;

    private static readonly byte[] s_jump =
    [
        0x00, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x03,
        0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x04, 0x04,
    ];

    private static readonly byte[] s_leap =
    [
        0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
    ];

    private readonly EntityTable _entities;

    internal EnemyJump(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities;
    }

    internal void StartUp(int slot, in PlayerCell cell, SolidMap map)
    {
        _entities.LeftFlag[slot] = 0;
        _entities.RightFlag[slot] = 0;
        _entities.Heading[slot] &= 0xFB;
        _entities.RiseCounter[slot] = WindUp;

        Step(slot, cell, map);
    }

    internal void Step(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte rise = _entities.RiseCounter[slot];

        if (rise == 0)
        {
            Fall(slot, cell, map);
            return;
        }

        _entities.RiseCounter[slot]--;

        if (rise >= RiseStart)
        {
            if ((rise & 0x03) == 0)
            {
                _entities.Frame[slot] ^= _entities.FrameCount[slot];
            }

            return;
        }

        if (Leaping(slot))
        {
            _entities.Y[slot]--;
            Across(slot, cell, map);
            return;
        }

        _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] - s_jump[rise]));
        EntityAnimation.Step(_entities, slot);
    }

    private static bool Moves(in PlayerCell cell, SolidMap map, int beside, int behind)
        => cell.FineX != 0 || !cell.Solid(map, beside) || cell.Solid(map, behind);

    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left) || cell.Solid(map, middle) || (cell.FineX != 0 && cell.Solid(map, right));

    private void Fall(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.FallCounter[slot] & 0x80) != 0)
        {
            _entities.FallCounter[slot] = 0;
        }

        byte fall = _entities.FallCounter[slot];

        if (fall == FallEnd)
        {
            End(slot);
            return;
        }

        _entities.FallCounter[slot]++;

        byte y = unchecked((byte)(_entities.Y[slot] + (Leaping(slot) ? s_leap[fall] : s_jump[fall])));
        _entities.Y[slot] = y;

        if (y < LandCheckFloor || (unchecked((byte)(y - TopEdge)) & 0x07) >= cell.FineY)
        {
            Across(slot, cell, map);
            return;
        }

        if (Blocked(cell, map, ClearRowLeft, ClearRowMiddle, ClearRowRight)
            || !Blocked(cell, map, FloorRowLeft, FloorRowMiddle, FloorRowRight))
        {
            Across(slot, cell, map);
            return;
        }

        _entities.Y[slot] = unchecked((byte)(((y - GridBase) & 0xF8) + GridBase));
        End(slot);
    }

    private void End(int slot)
    {
        _entities.RiseCounter[slot] = 0xFF;
        _entities.FallCounter[slot] = 0xFF;
        EntityAnimation.Step(_entities, slot);
    }

    private void Across(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.LeftFlag[slot] & 0x80) != 0)
        {
            int beside = cell.FineY == 0 ? LeftMiddle : LeftBottom;

            if (Moves(cell, map, beside, beside + 1))
            {
                _entities.X[slot] = unchecked((byte)(_entities.X[slot] - AcrossStep));
            }
            else
            {
                _entities.LeftFlag[slot] = 0x01;
            }
        }
        else if ((_entities.RightFlag[slot] & 0x80) != 0)
        {
            int beside = cell.FineY == 0 ? RightMiddle : RightBottom;

            if (_entities.X[slot] != RightEdge && Moves(cell, map, beside, beside - 1))
            {
                _entities.X[slot] = unchecked((byte)(_entities.X[slot] + AcrossStep));
            }
            else
            {
                _entities.RightFlag[slot] = 0x01;
            }
        }

        EntityAnimation.Step(_entities, slot);
    }

    private bool Leaping(int slot) => (_entities.LeftFlag[slot] | _entities.RightFlag[slot]) != 0;
}

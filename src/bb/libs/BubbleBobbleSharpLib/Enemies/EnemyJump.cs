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
        _entities.Slot(slot).LeftFlag = 0;
        _entities.Slot(slot).RightFlag = 0;
        _entities.Slot(slot).Heading &= 0xFB;
        _entities.Slot(slot).RiseCounter = WindUp;

        Step(slot, cell, map);
    }

    internal void Step(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte rise = _entities.Slot(slot).RiseCounter;

        if (rise == 0)
        {
            Fall(slot, cell, map);
            return;
        }

        _entities.Slot(slot).RiseCounter--;

        if (rise >= RiseStart)
        {
            if ((rise & 0x03) == 0)
            {
                _entities.Slot(slot).Frame ^= _entities.Slot(slot).FrameCount;
            }

            return;
        }

        if (Leaping(slot))
        {
            _entities.Slot(slot).Y--;
            Across(slot, cell, map);
            return;
        }

        _entities.Slot(slot).Y = unchecked((byte)(_entities.Slot(slot).Y - s_jump[rise]));
        EntityAnimation.Step(_entities, slot);
    }

    private static bool Moves(in PlayerCell cell, SolidMap map, int beside, int behind)
        => cell.FineX != 0 || !cell.Solid(map, beside) || cell.Solid(map, behind);

    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left) || cell.Solid(map, middle) || (cell.FineX != 0 && cell.Solid(map, right));

    private void Fall(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.Slot(slot).FallCounter & 0x80) != 0)
        {
            _entities.Slot(slot).FallCounter = 0;
        }

        byte fall = _entities.Slot(slot).FallCounter;

        if (fall == FallEnd)
        {
            End(slot);
            return;
        }

        _entities.Slot(slot).FallCounter++;

        byte y = unchecked((byte)(_entities.Slot(slot).Y + (Leaping(slot) ? s_leap[fall] : s_jump[fall])));
        _entities.Slot(slot).Y = y;

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

        _entities.Slot(slot).Y = unchecked((byte)(((y - GridBase) & 0xF8) + GridBase));
        End(slot);
    }

    private void End(int slot)
    {
        _entities.Slot(slot).RiseCounter = 0xFF;
        _entities.Slot(slot).FallCounter = 0xFF;
        EntityAnimation.Step(_entities, slot);
    }

    private void Across(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.Slot(slot).LeftFlag & 0x80) != 0)
        {
            int beside = cell.FineY == 0 ? LeftMiddle : LeftBottom;

            if (Moves(cell, map, beside, beside + 1))
            {
                _entities.Slot(slot).X = unchecked((byte)(_entities.Slot(slot).X - AcrossStep));
            }
            else
            {
                _entities.Slot(slot).LeftFlag = 0x01;
            }
        }
        else if ((_entities.Slot(slot).RightFlag & 0x80) != 0)
        {
            int beside = cell.FineY == 0 ? RightMiddle : RightBottom;

            if (_entities.Slot(slot).X != RightEdge && Moves(cell, map, beside, beside - 1))
            {
                _entities.Slot(slot).X = unchecked((byte)(_entities.Slot(slot).X + AcrossStep));
            }
            else
            {
                _entities.Slot(slot).RightFlag = 0x01;
            }
        }

        EntityAnimation.Step(_entities, slot);
    }

    private bool Leaping(int slot) => (_entities.Slot(slot).LeftFlag | _entities.Slot(slot).RightFlag) != 0;
}

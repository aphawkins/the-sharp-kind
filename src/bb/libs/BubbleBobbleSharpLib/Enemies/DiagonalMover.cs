// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class DiagonalMover
{
    private const byte LeftBit = 0x01;
    private const byte UpBit = 0x04;

    private const byte TurnAcross = 0x03;
    private const byte TurnUpOrDown = 0x0C;

    private const byte Slow = 0x01;
    private const byte Fast = 0x02;

    private const byte FacingLeft = 0x04;

    private const byte Across = 0x02;

    private const byte TopWrap = 0x15;
    private const byte BottomWrap = 0xF5;

    private const byte TopWrapOffset = 0x13;
    private const byte BottomWrapOffset = 0x14;

    private const int FloorRow = SolidMap.Rows - 1;
    private const int CeilingRow = 0;
    private const int WrapColumnBias = 1;

    private const int AboveLeft = 0x01;
    private const int AboveMiddle = 0x02;
    private const int AboveRight = 0x03;

    private const int BelowLeft = 0x51;
    private const int BelowMiddle = 0x52;
    private const int BelowRight = 0x53;

    private const int LeftTop = 0x00;
    private const int LeftMiddle = 0x28;
    private const int LeftBottom = 0x50;
    private const int RightTop = 0x03;
    private const int RightMiddle = 0x2B;
    private const int RightBottom = 0x53;

    private const byte AnimationPeriod = 0x02;
    private const byte FrameMask = 0x03;

    private readonly EntityTable _entities;

    internal DiagonalMover(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities;
    }

    internal void Step(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte heading = _entities.Heading[slot];
        PlayerCell moved = Vertically(slot, heading, cell, map, Slow);

        if ((heading & LeftBit) != 0)
        {
            StepLeft(slot, moved, map, false);
        }
        else
        {
            StepRight(slot, moved, map, false);
        }

        Animate(slot);
    }

    internal void StepFacing(int slot, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte facing = _entities.Frame[slot];
        byte heading = _entities.Heading[slot];
        PlayerCell moved = Vertically(slot, heading, cell, map, Fast);

        if ((heading & LeftBit) != 0)
        {
            if (facing < FacingLeft)
            {
                _entities.Frame[slot] = FacingLeft;
            }
            else
            {
                StepLeft(slot, moved, map, true);
            }
        }
        else if (facing >= FacingLeft)
        {
            _entities.Frame[slot] = 0;
        }
        else
        {
            StepRight(slot, moved, map, true);
        }

        EntityAnimation.Step(_entities, slot);
    }

    internal void Animate(int slot)
    {
        _entities.AnimationTimer[slot]++;

        if (_entities.AnimationTimer[slot] < AnimationPeriod)
        {
            return;
        }

        _entities.AnimationTimer[slot] = 0;
        _entities.Frame[slot] = (byte)((_entities.Frame[slot] + 1) & FrameMask);
    }

    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left) || cell.Solid(map, middle) || (cell.FineX != 0 && cell.Solid(map, right));

    private static bool Beside(in PlayerCell cell, SolidMap map, int top, int middle, int bottom)
        => cell.Solid(map, top) || cell.Solid(map, middle) || (cell.FineY != 0 && cell.Solid(map, bottom));

    private PlayerCell Vertically(int slot, byte heading, in PlayerCell cell, SolidMap map, byte speed)
    {
        if ((heading & UpBit) != 0)
        {
            StepUp(slot, cell, map, unchecked((byte)-speed));
        }
        else
        {
            StepDown(slot, cell, map, speed);
        }

        return PlayerCell.Of(_entities.X[slot], _entities.Y[slot]);
    }

    private void StepUp(int slot, in PlayerCell cell, SolidMap map, byte up)
    {
        if (cell.FineY != 0)
        {
            MoveVertically(slot, up);
            return;
        }

        byte y = _entities.Y[slot];

        if (y == TopWrap)
        {
            WrapThrough(slot, map, FloorRow, TopWrapOffset, BottomWrap);
            return;
        }

        PlayerCell above = cell with { Row = cell.Row - 1 };

        if (Blocked(above, map, AboveLeft, AboveMiddle, AboveRight))
        {
            TurnVertically(slot);
            return;
        }

        MoveVertically(slot, up);
    }

    private void StepDown(int slot, in PlayerCell cell, SolidMap map, byte down)
    {
        if (cell.FineY != 0)
        {
            MoveVertically(slot, down);
            return;
        }

        if (_entities.Y[slot] >= BottomWrap)
        {
            WrapThrough(slot, map, CeilingRow, BottomWrapOffset, TopWrap);
            return;
        }

        if (Blocked(cell, map, BelowLeft, BelowMiddle, BelowRight))
        {
            TurnVertically(slot);
            return;
        }

        MoveVertically(slot, down);
    }

    private void WrapThrough(int slot, SolidMap map, int row, byte offset, byte arrival)
    {
        int column = WrapColumnBias + (unchecked((byte)(_entities.X[slot] - offset)) >> 3);

        if (map[row, column])
        {
            TurnVertically(slot);
            return;
        }

        _entities.Y[slot] = arrival;
    }

    private void StepLeft(int slot, in PlayerCell cell, SolidMap map, bool faces)
    {
        if (cell.FineX == 0 && Beside(cell, map, LeftTop, LeftMiddle, LeftBottom))
        {
            TurnAcrossWay(slot, faces);
            return;
        }

        _entities.X[slot] = unchecked((byte)(_entities.X[slot] - Across));
    }

    private void StepRight(int slot, in PlayerCell cell, SolidMap map, bool faces)
    {
        if (cell.FineX == 0 && Beside(cell, map, RightTop, RightMiddle, RightBottom))
        {
            TurnAcrossWay(slot, faces);
            return;
        }

        _entities.X[slot] = unchecked((byte)(_entities.X[slot] + Across));
    }

    private void MoveVertically(int slot, byte step)
        => _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] + step));

    private void TurnVertically(int slot) => _entities.Heading[slot] ^= TurnUpOrDown;

    private void TurnAcrossWay(int slot, bool faces)
    {
        _entities.Heading[slot] ^= TurnAcross;

        if (faces)
        {
            _entities.Frame[slot] ^= FacingLeft;
        }
    }
}

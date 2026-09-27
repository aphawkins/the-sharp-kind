// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class EnemyWalker
{
    private const byte LeftBit = 0x01;
    private const byte RightBit = 0x02;
    private const byte UpBit = 0x04;

    private const byte GroundCheckFloor = 0x1F;

    private const int LeapRightFineX = 0x06;

    private const int LeapLeftGap = 0x50;
    private const int LeapRightGap = 0x53;
    private const int LeapLeftFar = 0x50 - 0x09;
    private const int LeapRightFar = 0x50 + 0x09;

    private const byte LeapRise = 0x0F;

    private const int FloorLeft = 0x51;
    private const int FloorMiddle = 0x52;
    private const int FloorRight = 0x53;

    private const int LeftMiddle = 0x28;
    private const int LeftBottom = 0x50;
    private const int RightMiddle = 0x2B;
    private const int RightBottom = 0x53;

    private const byte StepLeft = 0xFE;
    private const byte StepRight = 0x02;

    private const byte NoLeapState = 0x03;

    private readonly EntityTable _entities;
    private readonly EnemyChase _chase;
    private readonly EnemyJump _jump;
    private readonly PlayerDescent _descent;

    internal EnemyWalker(EntityTable entities, EnemyChase chase, EnemyJump jump, PlayerDescent descent)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(chase);
        ArgumentNullException.ThrowIfNull(jump);
        ArgumentNullException.ThrowIfNull(descent);

        _entities = entities;
        _chase = chase;
        _jump = jump;
        _descent = descent;
    }

    internal void Step(int slot, int target, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte facing = _entities.Frame[slot];

        _chase.Step(slot, target);

        byte heading = _entities.Heading[slot];

        if ((_entities.GroundState[slot] & 0x80) == 0)
        {
            Descend(slot, cell, map);
            return;
        }

        if ((_entities.RiseCounter[slot] & 0x80) == 0)
        {
            _jump.Step(slot, cell, map);
            return;
        }

        if (_entities.ClimbFlag[slot] != 0 && EnemyProbes.PlatformAbove(cell, map))
        {
            _entities.Heading[slot] |= UpBit;
            heading = _entities.Heading[slot];
        }

        if (_entities.Y[slot] < GroundCheckFloor)
        {
            StartFall(slot, cell, map);
            return;
        }

        if (cell.FineY != 0)
        {
            Walk(slot, facing, heading, cell, map);
            return;
        }

        if (!EnemyProbes.Blocked(cell, map, FloorLeft, FloorMiddle, FloorRight))
        {
            StartFall(slot, cell, map);
            return;
        }

        if (!Leap(slot, facing, cell, map))
        {
            Walk(slot, facing, heading, cell, map);
        }
    }

    private bool Leap(int slot, byte facing, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.LeapFlag[slot] | _entities.ClimbFlag[slot]) == 0 || _entities.State[slot] == NoLeapState)
        {
            return false;
        }

        bool left = facing >= _entities.FrameCount[slot];

        if (cell.FineX != (left ? 0 : LeapRightFineX))
        {
            return false;
        }

        if (cell.Solid(map, left ? LeapLeftGap : LeapRightGap) || !cell.Solid(map, left ? LeapLeftFar : LeapRightFar))
        {
            return false;
        }

        _entities.LeftFlag[slot] = left ? (byte)0xFF : (byte)0x00;
        _entities.RightFlag[slot] = left ? (byte)0x00 : (byte)0xFF;
        _entities.RiseCounter[slot] = LeapRise;

        _jump.Step(slot, cell, map);
        return true;
    }

    private void Walk(int slot, byte facing, byte heading, in PlayerCell cell, SolidMap map)
    {
        if ((heading & LeftBit) != 0)
        {
            WalkLeft(slot, facing, cell, map);
        }

        if ((heading & RightBit) != 0)
        {
            WalkRight(slot, facing, cell, map);
        }

        if ((heading & UpBit) != 0)
        {
            _jump.StartUp(slot, cell, map);
            return;
        }

        EntityAnimation.Step(_entities, slot);
    }

    private void WalkLeft(int slot, byte facing, in PlayerCell cell, SolidMap map)
    {
        if (facing < _entities.FrameCount[slot])
        {
            _entities.Frame[slot] = _entities.FrameCount[slot];
            return;
        }

        EnemyProbes.Sidestep(_entities, slot, cell, map, cell.FineY == 0 ? LeftMiddle : LeftBottom, StepLeft);
    }

    private void WalkRight(int slot, byte facing, in PlayerCell cell, SolidMap map)
    {
        if (facing >= _entities.FrameCount[slot])
        {
            _entities.Frame[slot] = 0;
            return;
        }

        EnemyProbes.Sidestep(_entities, slot, cell, map, cell.FineY == 0 ? RightMiddle : RightBottom, StepRight);
    }

    private void StartFall(int slot, in PlayerCell cell, SolidMap map)
    {
        _entities.GroundState[slot]++;
        Descend(slot, cell, map);
    }

    private void Descend(int slot, in PlayerCell cell, SolidMap map)
    {
        _descent.Step(slot, cell, map);
        EntityAnimation.Step(_entities, slot);
    }
}

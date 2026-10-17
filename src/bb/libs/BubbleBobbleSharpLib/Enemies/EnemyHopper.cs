// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class EnemyHopper
{
    private const byte GroundCheckFloor = 0x1F;

    private const int FloorLeft = 0x51;
    private const int FloorMiddle = 0x52;
    private const int FloorRight = 0x53;

    private const int BelowLeft = 0x79;
    private const int BelowMiddle = 0x7A;
    private const int BelowRight = 0x7B;

    private const byte UpRise = 0x1F;
    private const byte AcrossRise = 0x0F;

    private const byte RiseStart = 0x10;

    private const byte FallEnd = 0x10;

    private const byte TurnChance = 0x1E;

    private const byte FaceLeft = 0x07;
    private const byte FaceRight = 0x03;

    private const byte SpringMask = 0x03;
    private const byte Crouched = 0x02;

    private const byte LandCheckFloor = 0x1F;
    private const byte TopEdge = 0x15;
    private const byte GridBase = 0x2D;

    private const byte WallCheckFloor = 0x2D;

    private const int LeftTop = 0x00;
    private const int LeftMiddle = 0x28;
    private const int LeftBottom = 0x50;
    private const int RightTop = 0x03;
    private const int RightMiddle = 0x2B;
    private const int RightBottom = 0x53;

    private const byte AcrossStep = 0x02;

    private const byte FacingBit = 0x04;

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
    private readonly EnemyChase _chase;
    private readonly PlayerDescent _descent;
    private readonly BbRandom _random;

    internal EnemyHopper(EntityTable entities, EnemyChase chase, PlayerDescent descent, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(chase);
        ArgumentNullException.ThrowIfNull(descent);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _chase = chase;
        _descent = descent;
        _random = random;
    }

    internal void Step(int slot, int target, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        byte facing = _entities.Slot(slot).Frame;

        _chase.Step(slot, target);

        if ((_entities.Slot(slot).GroundState & 0x80) == 0)
        {
            Descend(slot, cell, map);
            return;
        }

        if ((_entities.Slot(slot).RiseCounter & 0x80) == 0)
        {
            Hop(slot, _entities.Slot(slot).RiseCounter, cell, map);
            return;
        }

        if (_entities.Slot(slot).Y < GroundCheckFloor || !EnemyProbes.Blocked(cell, map, FloorLeft, FloorMiddle, FloorRight))
        {
            _entities.Slot(slot).GroundState++;
            Descend(slot, cell, map);
            return;
        }

        if (_entities.Slot(slot).ClimbFlag != 0 && EnemyProbes.PlatformAbove(cell, map))
        {
            _entities.Slot(slot).LeftFlag = 0;
            _entities.Slot(slot).RightFlag = 0;
            _entities.Slot(slot).RiseCounter = UpRise;
            Hop(slot, UpRise, cell, map);
            return;
        }

        if (_entities.Slot(slot).LeapFlag != 0 && _random.Next() < TurnChance)
        {
            facing = _entities.Slot(target).X >= _entities.Slot(slot).X ? FaceRight : FaceLeft;
            _entities.Slot(slot).Frame = facing;
        }

        bool left = facing >= _entities.Slot(slot).FrameCount;
        _entities.Slot(slot).LeftFlag = left ? (byte)0xFF : (byte)0x00;
        _entities.Slot(slot).RightFlag = left ? (byte)0x00 : (byte)0xFF;
        _entities.Slot(slot).RiseCounter = AcrossRise;
        Hop(slot, AcrossRise, cell, map);
    }

    private void Hop(int slot, byte rise, in PlayerCell cell, SolidMap map)
    {
        if (rise == 0)
        {
            Fall(slot, cell, map);
            return;
        }

        if (rise >= RiseStart)
        {
            _entities.Slot(slot).RiseCounter--;

            if ((rise & 0x03) == 0)
            {
                _entities.Slot(slot).Frame = _entities.Slot(slot).Frame >= _entities.Slot(slot).FrameCount ? FaceRight : FaceLeft;
            }

            return;
        }

        if ((_entities.Slot(slot).Frame & SpringMask) >= Crouched)
        {
            _entities.Slot(slot).Frame--;
            return;
        }

        _entities.Slot(slot).RiseCounter--;

        if (Leaping(slot))
        {
            _entities.Slot(slot).Y--;
            Across(slot, cell, map);
            return;
        }

        _entities.Slot(slot).Y = unchecked((byte)(_entities.Slot(slot).Y - s_jump[rise]));
        EntityAnimation.Toggle(_entities, slot);
    }

    private void Fall(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.Slot(slot).FallCounter & 0x80) != 0)
        {
            _entities.Slot(slot).FallCounter = 0;
        }

        byte fall = _entities.Slot(slot).FallCounter;

        if (fall == FallEnd || (_entities.Slot(slot).Frame & SpringMask) >= Crouched)
        {
            Land(slot);
            return;
        }

        _entities.Slot(slot).FallCounter++;

        byte y = unchecked((byte)(_entities.Slot(slot).Y + (Leaping(slot) ? s_leap[fall] : s_jump[fall])));
        _entities.Slot(slot).Y = y;

        if (y < LandCheckFloor
            || (unchecked((byte)(y - TopEdge)) & 0x07) >= cell.FineY
            || EnemyProbes.Blocked(cell, map, FloorLeft, FloorMiddle, FloorRight)
            || !EnemyProbes.Blocked(cell, map, BelowLeft, BelowMiddle, BelowRight))
        {
            Across(slot, cell, map);
            return;
        }

        _entities.Slot(slot).Y = unchecked((byte)(((y - GridBase) & 0xF8) + GridBase));
        Land(slot);
    }

    private void Land(int slot)
    {
        int spring = _entities.Slot(slot).Frame & SpringMask;

        if (spring == SpringMask)
        {
            _entities.Slot(slot).RiseCounter = 0xFF;
            _entities.Slot(slot).FallCounter = 0xFF;
            EntityAnimation.Toggle(_entities, slot);
            return;
        }

        _entities.Slot(slot).Frame = spring >= Crouched
            ? (byte)(_entities.Slot(slot).Frame + 1)
            : (byte)((_entities.Slot(slot).Frame & FacingBit) | Crouched);
    }

    private void Across(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.Slot(slot).LeftFlag & 0x80) != 0)
        {
            if (Walled(slot, cell, map, LeftTop, LeftMiddle, LeftBottom))
            {
                _entities.Slot(slot).LeftFlag = 0;
                _entities.Slot(slot).RightFlag--;
                _entities.Slot(slot).Frame ^= FacingBit;
            }
            else
            {
                _entities.Slot(slot).X = unchecked((byte)(_entities.Slot(slot).X - AcrossStep));
            }
        }
        else if ((_entities.Slot(slot).RightFlag & 0x80) != 0)
        {
            if (Walled(slot, cell, map, RightTop, RightMiddle, RightBottom))
            {
                _entities.Slot(slot).RightFlag = 0;
                _entities.Slot(slot).LeftFlag--;
                _entities.Slot(slot).Frame ^= FacingBit;
            }
            else
            {
                _entities.Slot(slot).X = unchecked((byte)(_entities.Slot(slot).X + AcrossStep));
            }
        }

        EntityAnimation.Toggle(_entities, slot);
    }

    private bool Walled(int slot, in PlayerCell cell, SolidMap map, int top, int middle, int bottom)
        => _entities.Slot(slot).Y >= WallCheckFloor
            && cell.FineX == 0
            && (cell.Solid(map, top) || cell.Solid(map, middle) || (cell.FineY != 0 && cell.Solid(map, bottom)));

    private void Descend(int slot, in PlayerCell cell, SolidMap map)
    {
        byte ground = _entities.Slot(slot).GroundState;

        _descent.Step(slot, cell, map);

        if (_entities.Slot(slot).GroundState != ground)
        {
            EntityAnimation.Step(_entities, slot);
        }
    }

    private bool Leaping(int slot) => (_entities.Slot(slot).LeftFlag | _entities.Slot(slot).RightFlag) != 0;
}

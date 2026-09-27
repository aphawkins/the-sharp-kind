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

        byte facing = _entities.Frame[slot];

        _chase.Step(slot, target);

        if ((_entities.GroundState[slot] & 0x80) == 0)
        {
            Descend(slot, cell, map);
            return;
        }

        if ((_entities.RiseCounter[slot] & 0x80) == 0)
        {
            Hop(slot, _entities.RiseCounter[slot], cell, map);
            return;
        }

        if (_entities.Y[slot] < GroundCheckFloor || !EnemyProbes.Blocked(cell, map, FloorLeft, FloorMiddle, FloorRight))
        {
            _entities.GroundState[slot]++;
            Descend(slot, cell, map);
            return;
        }

        if (_entities.ClimbFlag[slot] != 0 && EnemyProbes.PlatformAbove(cell, map))
        {
            _entities.LeftFlag[slot] = 0;
            _entities.RightFlag[slot] = 0;
            _entities.RiseCounter[slot] = UpRise;
            Hop(slot, UpRise, cell, map);
            return;
        }

        if (_entities.LeapFlag[slot] != 0 && _random.Next() < TurnChance)
        {
            facing = _entities.X[target] >= _entities.X[slot] ? FaceRight : FaceLeft;
            _entities.Frame[slot] = facing;
        }

        bool left = facing >= _entities.FrameCount[slot];
        _entities.LeftFlag[slot] = left ? (byte)0xFF : (byte)0x00;
        _entities.RightFlag[slot] = left ? (byte)0x00 : (byte)0xFF;
        _entities.RiseCounter[slot] = AcrossRise;
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
            _entities.RiseCounter[slot]--;

            if ((rise & 0x03) == 0)
            {
                _entities.Frame[slot] = _entities.Frame[slot] >= _entities.FrameCount[slot] ? FaceRight : FaceLeft;
            }

            return;
        }

        if ((_entities.Frame[slot] & SpringMask) >= Crouched)
        {
            _entities.Frame[slot]--;
            return;
        }

        _entities.RiseCounter[slot]--;

        if (Leaping(slot))
        {
            _entities.Y[slot]--;
            Across(slot, cell, map);
            return;
        }

        _entities.Y[slot] = unchecked((byte)(_entities.Y[slot] - s_jump[rise]));
        EntityAnimation.Toggle(_entities, slot);
    }

    private void Fall(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.FallCounter[slot] & 0x80) != 0)
        {
            _entities.FallCounter[slot] = 0;
        }

        byte fall = _entities.FallCounter[slot];

        if (fall == FallEnd || (_entities.Frame[slot] & SpringMask) >= Crouched)
        {
            Land(slot);
            return;
        }

        _entities.FallCounter[slot]++;

        byte y = unchecked((byte)(_entities.Y[slot] + (Leaping(slot) ? s_leap[fall] : s_jump[fall])));
        _entities.Y[slot] = y;

        if (y < LandCheckFloor
            || (unchecked((byte)(y - TopEdge)) & 0x07) >= cell.FineY
            || EnemyProbes.Blocked(cell, map, FloorLeft, FloorMiddle, FloorRight)
            || !EnemyProbes.Blocked(cell, map, BelowLeft, BelowMiddle, BelowRight))
        {
            Across(slot, cell, map);
            return;
        }

        _entities.Y[slot] = unchecked((byte)(((y - GridBase) & 0xF8) + GridBase));
        Land(slot);
    }

    private void Land(int slot)
    {
        int spring = _entities.Frame[slot] & SpringMask;

        if (spring == SpringMask)
        {
            _entities.RiseCounter[slot] = 0xFF;
            _entities.FallCounter[slot] = 0xFF;
            EntityAnimation.Toggle(_entities, slot);
            return;
        }

        _entities.Frame[slot] = spring >= Crouched
            ? (byte)(_entities.Frame[slot] + 1)
            : (byte)((_entities.Frame[slot] & FacingBit) | Crouched);
    }

    private void Across(int slot, in PlayerCell cell, SolidMap map)
    {
        if ((_entities.LeftFlag[slot] & 0x80) != 0)
        {
            if (Walled(slot, cell, map, LeftTop, LeftMiddle, LeftBottom))
            {
                _entities.LeftFlag[slot] = 0;
                _entities.RightFlag[slot]--;
                _entities.Frame[slot] ^= FacingBit;
            }
            else
            {
                _entities.X[slot] = unchecked((byte)(_entities.X[slot] - AcrossStep));
            }
        }
        else if ((_entities.RightFlag[slot] & 0x80) != 0)
        {
            if (Walled(slot, cell, map, RightTop, RightMiddle, RightBottom))
            {
                _entities.RightFlag[slot] = 0;
                _entities.LeftFlag[slot]--;
                _entities.Frame[slot] ^= FacingBit;
            }
            else
            {
                _entities.X[slot] = unchecked((byte)(_entities.X[slot] + AcrossStep));
            }
        }

        EntityAnimation.Toggle(_entities, slot);
    }

    private bool Walled(int slot, in PlayerCell cell, SolidMap map, int top, int middle, int bottom)
        => _entities.Y[slot] >= WallCheckFloor
            && cell.FineX == 0
            && (cell.Solid(map, top) || cell.Solid(map, middle) || (cell.FineY != 0 && cell.Solid(map, bottom)));

    private void Descend(int slot, in PlayerCell cell, SolidMap map)
    {
        byte ground = _entities.GroundState[slot];

        _descent.Step(slot, cell, map);

        if (_entities.GroundState[slot] != ground)
        {
            EntityAnimation.Step(_entities, slot);
        }
    }

    private bool Leaping(int slot) => (_entities.LeftFlag[slot] | _entities.RightFlag[slot]) != 0;
}

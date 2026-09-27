// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerDrift
{
    private const byte LeftEdge = 0x24;
    private const byte LeftEdgeLimit = 0x25;
    private const byte RightEdge = 0xF4;

    private const byte WallCheckFloor = 0x2D;

    private const int LeftProbeAligned = 0x28;
    private const int LeftProbeSplit = 0x50;
    private const int RightProbeAligned = 0x2B;
    private const int RightProbeSplit = 0x53;

    private const byte AnimationPeriod = 0x02;

    private const byte AnimationCeiling = 0x08;

    private readonly EntityTable _entities;
    private readonly PlayerSteer _steer;
    private readonly Rings _rings;

    internal PlayerDrift(EntityTable entities, PlayerSteer steer, Rings rings)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(steer);
        ArgumentNullException.ThrowIfNull(rings);

        _entities = entities;
        _steer = steer;
        _rings = rings;
    }

    internal void Enter(int player, byte port, in PlayerCell cell, SolidMap map)
    {
        _rings.Drift(player);
        Step(player, port, cell, map);
    }

    internal void Step(int player, byte port, in PlayerCell cell, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        if ((_entities.LeftFlag[player] & 0x80) != 0)
        {
            Left(player, cell, map);
        }
        else if ((_entities.RightFlag[player] & 0x80) != 0)
        {
            Right(player, cell, map);
        }

        Animate(player, port, map);
    }

    private static bool Clear(in PlayerCell cell, SolidMap map, int probe, int beyond)
        => !cell.Solid(map, probe) || cell.Solid(map, beyond);

    private void Left(int player, in PlayerCell cell, SolidMap map)
    {
        if (_entities.X[player] < LeftEdgeLimit)
        {
            _entities.X[player] = LeftEdge;
            _entities.LeftFlag[player] = 0x00;
            return;
        }

        int probe = cell.FineY == 0 ? LeftProbeAligned : LeftProbeSplit;

        if (Open(player, cell, map, probe, probe + 1))
        {
            _entities.X[player]--;
            return;
        }

        _entities.LeftFlag[player] = 0x00;
    }

    private void Right(int player, in PlayerCell cell, SolidMap map)
    {
        if (_entities.X[player] >= RightEdge)
        {
            _entities.X[player] = RightEdge;
            _entities.RightFlag[player] = 0x00;
            return;
        }

        int probe = cell.FineY == 0 ? RightProbeAligned : RightProbeSplit;

        if (Open(player, cell, map, probe, probe - 1))
        {
            _entities.X[player]++;
            return;
        }

        _entities.RightFlag[player] = 0x00;
    }

    private bool Open(int player, in PlayerCell cell, SolidMap map, int probe, int beyond)
        => _entities.Y[player] < WallCheckFloor
            || cell.FineX != 0
            || Clear(cell, map, probe, beyond);

    private void Animate(int player, byte port, SolidMap map)
    {
        _entities.AnimationTimer[player]++;

        if (_entities.AnimationTimer[player] < AnimationPeriod)
        {
            return;
        }

        _entities.AnimationTimer[player] = 0;

        byte frame = _entities.Frame[player];

        if (frame >= AnimationCeiling)
        {
            return;
        }

        _entities.Frame[player] = (byte)(frame ^ 0x01);

        _steer.Step(player, port, map);
    }
}

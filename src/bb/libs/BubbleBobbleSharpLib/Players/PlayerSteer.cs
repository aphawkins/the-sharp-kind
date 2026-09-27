// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerSteer
{
    private const byte LeftEdge = 0x24;
    private const byte RightEdge = 0xF4;

    private const byte WallCheckFloor = 0x2D;

    private const int LeftProbeAligned = 0x28;
    private const int LeftProbeSplit = 0x50;
    private const int RightProbeAligned = 0x2B;
    private const int RightProbeSplit = 0x53;

    private const byte FaceLeft = 0x04;
    private const byte FaceRight = 0x00;

    private readonly EntityTable _entities;
    private readonly BubbleBlow _blow;

    internal PlayerSteer(EntityTable entities, BubbleBlow blow)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(blow);

        _entities = entities;
        _blow = blow;
    }

    internal void Step(int player, byte port, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        PlayerCell cell = PlayerCell.Of(_entities.X[player], _entities.Y[player]);

        if ((port & Input.Fire) == 0)
        {
            _blow.Blow(player);
        }

        byte frame = _entities.Frame[player];

        if ((port & Input.Left) == 0)
        {
            if (MovesLeft(frame))
            {
                Left(player, cell, map);
            }
            else
            {
                Turn(player, FaceLeft);
            }

            return;
        }

        if ((port & Input.Right) != 0)
        {
            return;
        }

        if (MovesRight(frame))
        {
            Right(player, cell, map);
        }
        else
        {
            Turn(player, FaceRight);
        }
    }

    private static bool MovesLeft(byte frame) => frame is (>= 0x04 and < 0x08) or >= 0x0A;

    private static bool MovesRight(byte frame) => frame is < 0x04 or 0x08 or 0x09;

    private void Left(int player, in PlayerCell cell, SolidMap map)
    {
        if (_entities.X[player] == LeftEdge)
        {
            return;
        }

        bool aligned = cell.FineY == 0;

        if (!aligned && _entities.Y[player] < WallCheckFloor)
        {
            _entities.X[player]--;
            return;
        }

        if (cell.FineX != 0 || !cell.Solid(map, aligned ? LeftProbeAligned : LeftProbeSplit))
        {
            _entities.X[player]--;
        }
    }

    private void Right(int player, in PlayerCell cell, SolidMap map)
    {
        if (_entities.X[player] == RightEdge)
        {
            return;
        }

        bool aligned = cell.FineY == 0;

        if (!aligned && _entities.Y[player] < WallCheckFloor)
        {
            _entities.X[player]++;
            return;
        }

        if (cell.FineX != 0 || !cell.Solid(map, aligned ? RightProbeAligned : RightProbeSplit))
        {
            _entities.X[player]++;
        }
    }

    private void Turn(int player, byte facing)
    {
        if ((_entities.BubbleTimer[player] & 0x80) == 0)
        {
            return;
        }

        _entities.Frame[player] = facing;
    }
}

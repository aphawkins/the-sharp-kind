// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerMovement
{
    private const byte LeftStep = 0xFE;
    private const byte RightStep = 0x02;

    private const byte FaceLeft = 0x04;
    private const byte FaceRight = 0x00;

    private const int LeftProbeAligned = 0x28;
    private const int LeftProbeSplit = 0x50;
    private const int RightProbeAligned = 0x2B;
    private const int RightProbeSplit = 0x53;

    private const byte WallCheckFloor = 0x2D;

    private const byte AnimationPeriod = 0x04;

    private const byte JumpRise = 0x0F;
    private const byte Latched = 0xFF;

    private readonly EntityTable _entities;
    private readonly BubbleBlow _blow;
    private readonly Rings _rings;

    internal PlayerMovement(EntityTable entities, BubbleBlow blow, Rings rings)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(blow);
        ArgumentNullException.ThrowIfNull(rings);

        _entities = entities;
        _blow = blow;
        _rings = rings;
    }

    internal void Step(int player, byte port, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        if ((port & Input.Up) == 0)
        {
            Launch(player, port);
            return;
        }

        PlayerCell cell = PlayerCell.Of(_entities.X[player], _entities.Y[player]);

        if ((port & Input.Left) == 0)
        {
            Move(player, cell, map, LeftStep);
        }

        if ((port & Input.Right) == 0)
        {
            Move(player, cell, map, RightStep);
        }

        if ((port & Input.Fire) == 0)
        {
            _blow.Blow(player);
        }
    }

    private static bool Faces(bool left, byte frame)
        => left ? frame is 0x04 or 0x05 or 0x0A or 0x0B : frame is < 0x02 or 0x08 or 0x09;

    private static bool Animates(bool left, byte frame)
        => left ? frame is 0x04 or 0x05 : frame is < 0x02;

    private static int Probe(bool left, bool aligned)
        => left
            ? aligned ? LeftProbeAligned : LeftProbeSplit
            : aligned ? RightProbeAligned : RightProbeSplit;

    private void Move(int player, in PlayerCell cell, SolidMap map, byte step)
    {
        bool left = step == LeftStep;

        byte frame = _entities.Frame[player];

        if (!Faces(left, frame))
        {
            Turn(player, left);
            return;
        }

        if (Animates(left, frame))
        {
            Animate(player);
        }

        Advance(player, cell, map, Probe(left, cell.FineY == 0), step);
    }

    private void Turn(int player, bool left)
    {
        if ((_entities.BubbleTimer[player] & 0x80) == 0)
        {
            return;
        }

        _entities.Frame[player] = left ? FaceLeft : FaceRight;
    }

    private void Launch(int player, byte port)
    {
        _entities.RiseCounter[player] = JumpRise;

        byte frame = _entities.Frame[player];

        bool left = (port & Input.Left) == 0 && frame is >= 0x04 and < 0x08;
        bool right = (port & Input.Right) == 0 && frame is < 0x04;

        _entities.LeftFlag[player] = left ? Latched : (byte)0x00;
        _entities.RightFlag[player] = right ? Latched : (byte)0x00;

        if (frame < 0x08)
        {
            _entities.Frame[player] = (byte)(frame | 0x02);
        }
    }

    private void Animate(int player)
    {
        _entities.AnimationTimer[player]++;

        if (_entities.AnimationTimer[player] < AnimationPeriod)
        {
            return;
        }

        _entities.AnimationTimer[player] = 0;
        _entities.Frame[player] ^= 0x01;
    }

    private void Advance(int player, in PlayerCell cell, SolidMap map, int probe, byte step)
    {
        bool checkWall = _entities.Y[player] >= WallCheckFloor && cell.FineX == 0;

        if (checkWall && cell.Solid(map, probe))
        {
            return;
        }

        _rings.Walk(player);
        _entities.X[player] = unchecked((byte)(_entities.X[player] + step));
    }
}

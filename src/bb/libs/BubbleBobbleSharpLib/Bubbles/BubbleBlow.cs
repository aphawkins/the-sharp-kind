// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Bubbles;

internal sealed class BubbleBlow
{
    internal const byte BubbleType = 0x16;

    internal const byte BlowFrames = 0x06;

    internal const byte SpawnTimer = 0x04;

    private const byte LeftEdge = 0x14;
    private const byte TopEdge = 0x15;

    private const byte FacingRightSprite = 0x09;

    private const int LowInCell = 0x04;

    private static readonly byte[] s_blowSprite = [0x00, 0x08, 0x08, 0x09, 0x09, 0x08, 0x08];

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly Rings _rings;

    internal BubbleBlow(PlayerTable players, EntityTable entities, ObjectTable objects, Rings rings)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(rings);

        _players = players;
        _entities = entities;
        _objects = objects;
        _rings = rings;
    }

    internal void Tick()
    {
        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            if (_players.Reload[player] != 0)
            {
                _players.Reload[player]--;
            }
        }
    }

    internal void Blow(int player)
    {
        if (_players.Reload[player] != 0)
        {
            return;
        }

        if ((_entities.BubbleTimer[player] & 0x80) == 0)
        {
            return;
        }

        _entities.BubbleTimer[player] = BlowFrames;

        _players.BlowFacing[player] = (byte)((_entities.Frame[player] & 0x04) >> 1);
    }

    internal void Step(int player, byte timer, in PlayerCell cell)
    {
        if (timer == 0)
        {
            _entities.Frame[player] = (byte)(_players.BlowFacing[player] << 1);
            _entities.BubbleTimer[player]--;
            return;
        }

        _entities.Frame[player] = (byte)(_players.BlowFacing[player] + s_blowSprite[timer]);
        _entities.BubbleTimer[player]--;

        if (timer != SpawnTimer)
        {
            return;
        }

        Spawn(player, cell);
    }

    private void Spawn(int player, in PlayerCell cell)
    {
        int slot = _objects.FindFree();

        if (slot < 0)
        {
            return;
        }

        byte x = _entities.X[player];
        byte acrossX = (byte)(x - LeftEdge);

        _objects.X[slot] = x;
        _objects.SubX[slot] = (byte)((acrossX & 0x06) >> 1);
        _objects.Column[slot] = (byte)(acrossX >> 3);

        byte y = _entities.Y[player];
        int downY = y - TopEdge;

        _objects.Y[slot] = y;

        if (downY < 0)
        {
            return;
        }

        _objects.SubY[slot] = (byte)(downY & 0x06);
        _objects.Row[slot] = (byte)(downY >> 3);

        _objects.Type[slot] = BubbleType;
        _objects.Flags[slot] = 0x7D;
        _objects.Behaviour[slot] = 0x7D;

        _players.Reload[player] = _players.BlowReload[player];
        _objects.EnemyType[slot] = _players.BlowType[player];
        _objects.State[slot] = _players.BlowState[player];
        _objects.Variant[slot] = _players.BlowVariant[player];

        Place(player, slot, cell);

        _rings.Blow(player);
    }

    private void Place(int player, int slot, in PlayerCell cell)
    {
        if (_entities.Frame[player] == FacingRightSprite)
        {
            _objects.Column[slot]++;
            _objects.X[slot] += 8;
            _objects.Direction[slot] = 0x00;
        }
        else
        {
            _objects.Direction[slot] = 0x80;
        }

        if ((_entities.RiseCounter[player] & 0x80) != 0 || cell.FineY < LowInCell)
        {
            return;
        }

        _objects.Row[slot]++;
        _objects.Y[slot] += 8;
    }
}

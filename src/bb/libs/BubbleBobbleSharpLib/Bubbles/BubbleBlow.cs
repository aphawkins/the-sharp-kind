// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Bubbles;

internal sealed class BubbleBlow
{
    internal const byte BubbleType = 0x16;

    internal const byte BlowFrames = 0x06;

    internal const byte SpawnTimer = 0x04;

    internal const byte ThunderType = 0x44;

    private const byte LeftEdge = 0x14;
    private const byte TopEdge = 0x15;

    private const byte FacingRightSprite = 0x09;

    private const int LowInCell = 0x04;

    private static readonly byte[] s_blowSprite = [0x00, 0x08, 0x08, 0x09, 0x09, 0x08, 0x08];

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly Rings _rings;
    private readonly RoundState _round;

    internal BubbleBlow(PlayerTable players, EntityTable entities, ObjectTable objects, Rings rings, RoundState? round = null)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(rings);

        _players = players;
        _entities = entities;
        _objects = objects;
        _rings = rings;
        _round = round ?? new();
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

        if ((_entities.Slot(player).BubbleTimer & 0x80) == 0)
        {
            return;
        }

        _entities.Slot(player).BubbleTimer = BlowFrames;

        _players.BlowFacing[player] = (byte)((_entities.Slot(player).Frame & 0x04) >> 1);
    }

    internal void Step(int player, byte timer, in PlayerCell cell)
    {
        if (timer == 0)
        {
            _entities.Slot(player).Frame = (byte)(_players.BlowFacing[player] << 1);
            _entities.Slot(player).BubbleTimer--;
            return;
        }

        _entities.Slot(player).Frame = (byte)(_players.BlowFacing[player] + s_blowSprite[timer]);
        _entities.Slot(player).BubbleTimer--;

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

        byte x = _entities.Slot(player).X;
        byte acrossX = (byte)(x - LeftEdge);

        _objects.Slot(slot).X = x;
        _objects.Slot(slot).SubX = (byte)((acrossX & 0x06) >> 1);
        _objects.Slot(slot).Column = (byte)(acrossX >> 3);

        byte y = _entities.Slot(player).Y;
        int downY = y - TopEdge;

        _objects.Slot(slot).Y = y;

        if (downY < 0)
        {
            return;
        }

        _objects.Slot(slot).SubY = (byte)(downY & 0x06);
        _objects.Slot(slot).Row = (byte)(downY >> 3);

        _objects.Slot(slot).Type = BubbleType;
        _objects.Slot(slot).Flags = 0x7D;
        _objects.Slot(slot).Behaviour = 0x7D;

        _players.Reload[player] = _players.BlowReload[player];
        _objects.Slot(slot).EnemyType = _players.BlowType[player];
        _objects.Slot(slot).State = _players.BlowState[player];
        _objects.Slot(slot).Variant = _players.BlowVariant[player];

        Place(player, slot, cell);
        Thunder(player, slot);

        _rings.Blow(player);

        // $23DE-$23E6, once the last level has patched it in: a marked player's bubble lives $08 or $09 more turns.
        if (_round.ThunderBlow && (sbyte)_players.BossBlow[player] < 0)
        {
            _objects.Slot(slot).Flags = (byte)(player | 0x08);
        }
    }

    // $23B8-$23D6: a player with thunder shots left blows one, which goes the way they face.
    private void Thunder(int player, int slot)
    {
        if ((_players.Shots[player] & 0x80) != 0)
        {
            return;
        }

        _players.Shots[player]--;
        _objects.Slot(slot).SubX = (byte)((_objects.Slot(slot).Direction & 0x80) != 0 ? 0 : 2);
        _objects.Slot(slot).State = 0;
        _objects.Slot(slot).Type = ThunderType;
    }

    private void Place(int player, int slot, in PlayerCell cell)
    {
        if (_entities.Slot(player).Frame == FacingRightSprite)
        {
            _objects.Slot(slot).Column++;
            _objects.Slot(slot).X += 8;
            _objects.Slot(slot).Direction = 0x00;
        }
        else
        {
            _objects.Slot(slot).Direction = 0x80;
        }

        if ((_entities.Slot(player).RiseCounter & 0x80) != 0 || cell.FineY < LowInCell)
        {
            return;
        }

        _objects.Slot(slot).Row++;
        _objects.Slot(slot).Y += 8;
    }
}

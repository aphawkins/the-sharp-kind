// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Specials;

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerFrame
{
    internal const byte PlayingState = 0x01;

    internal const byte DyingState = 0x0E;
    internal const byte DeadState = 0x0F;

    private const byte AngerFrames = 0x02;

    private const int GroundRowLeft = 0x51;
    private const int GroundRowMiddle = 0x52;
    private const int GroundRowRight = 0x53;

    private const byte StickMask = 0x1F;
    private const byte StickIdle = 0x1F;

    private const byte IdlePeriod = 0x07;

    private const byte RespawnFlash = 0x05;

    private readonly EntityTable _entities;
    private readonly PlayerMovement _movement;
    private readonly PlayerJump _jump;
    private readonly PlayerFall _fall;
    private readonly BubbleBlow _blow;
    private readonly PlayerDying _dying;
    private readonly WaterFlow _water;

    internal PlayerFrame(
        EntityTable entities,
        PlayerMovement movement,
        PlayerJump jump,
        PlayerFall fall,
        BubbleBlow blow,
        WaterFlow water)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(jump);
        ArgumentNullException.ThrowIfNull(fall);
        ArgumentNullException.ThrowIfNull(blow);
        ArgumentNullException.ThrowIfNull(water);

        _entities = entities;
        _movement = movement;
        _jump = jump;
        _fall = fall;
        _blow = blow;
        _water = water;
        _dying = new(entities);
    }

    internal void Step(ReadOnlySpan<byte> ports, byte counter, SolidMap map)
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            Slot(player, ports[player], counter, map);
        }
    }

    internal void Step(int player, byte port, SolidMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        switch (_entities.State[player])
        {
            case 0:
                return;
            case PlayingState:
                Playing(player, port, PlayerCell.Of(_entities.X[player], _entities.Y[player]), map);
                return;
            case DyingState:
                _dying.Step(player);
                return;
            case DeadState:
                return;
            case PlayerRespawn.RespawningState:
                Respawning(player, port, map);
                return;
            case WaterFlow.CarriedState:
                Carried(player, port, map);
                return;
            default:
                throw new NotSupportedException(
                    $"slot {player}'s state ${_entities.State[player]:X2} at $1E6C is not translated");
        }
    }

    internal void Slot(int player, byte port, byte counter, SolidMap map)
    {
        if (_entities.State[player] == 0)
        {
            return;
        }

        if (_entities.FlashTimer[player] != 0 && (counter & AngerFrames) == 0)
        {
            Step(player, port, map);
        }

        if (_entities.HoldTimer[player] != 0)
        {
            throw new NotSupportedException($"slot {player} with $8638 set at $1D11 is not translated");
        }

        Step(player, port, map);
    }

    private static bool Blocked(in PlayerCell cell, SolidMap map, int left, int middle, int right)
        => cell.Solid(map, left)
            || cell.Solid(map, middle)
            || (cell.FineX != 0 && cell.Solid(map, right));

    private void Playing(int player, byte port, in PlayerCell cell, SolidMap map)
    {
        byte blowTimer = _entities.BubbleTimer[player];

        if ((blowTimer & 0x80) == 0)
        {
            _blow.Step(player, blowTimer, cell);
        }

        if ((_entities.RiseCounter[player] & 0x80) == 0)
        {
            _jump.Step(player, port, cell, map);
            return;
        }

        if ((_entities.GroundState[player] & 0x80) == 0)
        {
            _fall.Step(player, port, cell, map);
            return;
        }

        if (cell.FineY == 0 && !Blocked(cell, map, GroundRowLeft, GroundRowMiddle, GroundRowRight))
        {
            _entities.GroundState[player]++;
            _fall.Step(player, port, cell, map);
            return;
        }

        if ((port & StickMask) != StickIdle)
        {
            _movement.Step(player, port, map);
            return;
        }

        Idle(player, port, map);
    }

    // $284D-$2877 and $287C-$28A4: carried by the water. A player at the water's bottom is let go; otherwise it can still
    // blow and walk, and a jump ends it.
    private void Carried(int player, byte port, SolidMap map)
    {
        if (_water.Follow(player))
        {
            _water.Release(player);
            _entities.State[player] = PlayingState;
            return;
        }

        byte blowTimer = _entities.BubbleTimer[player];

        if ((blowTimer & 0x80) == 0)
        {
            _blow.Step(player, blowTimer, PlayerCell.Of(_entities.X[player], _entities.Y[player]));
        }

        _movement.Step(player, port, map);

        if ((_entities.RiseCounter[player] & 0x80) == 0)
        {
            _entities.State[player] = PlayingState;
        }
    }

    // $290D: counts down to state 1, flashing, and plays as state 1 meanwhile.
    private void Respawning(int player, byte port, SolidMap map)
    {
        if (--_entities.TurnInterval[player] == 0)
        {
            _entities.State[player] = PlayingState;
        }

        _entities.Colour[player] ^= RespawnFlash;
        Playing(player, port, PlayerCell.Of(_entities.X[player], _entities.Y[player]), map);
    }

    private void Idle(int player, byte port, SolidMap map)
    {
        if ((_entities.BubbleTimer[player] & 0x80) == 0)
        {
            _movement.Step(player, port, map);
            return;
        }

        _entities.AnimationTimer[player]++;

        if (_entities.AnimationTimer[player] != IdlePeriod)
        {
            return;
        }

        _entities.Frame[player] ^= 0x01;
        _entities.AnimationTimer[player] = 0;
    }
}

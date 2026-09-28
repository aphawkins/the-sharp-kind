// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerRespawn
{
    internal const byte RespawningState = 0x10;

    internal const byte SpawnY = 0xDD;

    private const byte RespawnPasses = 0x4A;

    private const byte BonusLevel = 0x63;

    private const byte AngerFrom = 2;

    private readonly EntityTable _entities;
    private readonly PlayerTable _players;
    private readonly LevelItems _items;
    private readonly LevelTimer _timer;

    internal PlayerRespawn(EntityTable entities, PlayerTable players, LevelItems items, LevelTimer timer)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(timer);

        _entities = entities;
        _players = players;
        _items = items;
        _timer = timer;
    }

    internal static ReadOnlySpan<byte> SpawnX => [0x2C, 0xEC];

    internal static ReadOnlySpan<byte> SpawnFrame => [0x00, 0x04];

    private static ReadOnlySpan<byte> PlayerBit => [0x01, 0x02];

    internal void Update(byte subflg)
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (_entities.State[player] == PlayerFrame.DeadState)
            {
                Respawn(player, subflg);
            }
        }
    }

    private void Respawn(int player, byte subflg)
    {
        ResetPlayer(player, subflg);

        // $04A3's level music restart is not modelled: there is no music yet (step 8).
        _items.Undying &= (byte)~PlayerBit[player];
        _entities.Frame[player] = SpawnFrame[player];
        _entities.RiseCounter[player] = 0xFF;
        _entities.FallCounter[player] = 0xFF;
        _entities.GroundState[player] = 0xFF;

        if ((sbyte)--_players.Lives[player] < 0)
        {
            GameOver(player, subflg);
        }
        else
        {
            _entities.Y[player] = SpawnY;
            _entities.X[player] = SpawnX[player];
            _entities.TurnInterval[player] = RespawnPasses;

            // $04EB: in a bonus round ($5A7F) the player goes to state 0 instead; bonus rounds are step 7.
            _entities.State[player] = RespawningState;
        }

        _timer.Restart();

        if (_entities.EnemyCount >= AngerFrom)
        {
            _entities.SetAnger(0);
        }
    }

    // $04F0: the level reached is kept, and the player leaves the game at (0, 0) in state 0. $7BE8 writes
    // GAME OVER over the player's lives, which the HUD does for any count below zero.
    private void GameOver(int player, byte subflg)
    {
        _players.Round[player] = subflg;
        _entities.X[player] = 0;
        _entities.Y[player] = 0;
        _entities.State[player] = 0;
    }

    // $7F53.
    private void ResetPlayer(int player, byte subflg)
    {
        _players.BlowState[player] = 0x88;
        _players.BlowReload[player] = 0x08;
        _players.BlowVariant[player] = 0x04;
        _players.BlowType[player] = 0x04;

        // $A783, $37C7 and $A813 feed arms gated off by level setup; they are not modelled.
        _players.DriftRing[player] = 0;
        _players.WalkRing[player] = 0;
        _players.BlowRing[player] = 0;
        _entities.FlashTimer[player] = 0;
        _entities.LeapFlag[player] = 0;

        if (subflg == BonusLevel)
        {
            throw new NotSupportedException($"slot {player}'s death on the bonus level at $7F7F is not translated");
        }
    }
}

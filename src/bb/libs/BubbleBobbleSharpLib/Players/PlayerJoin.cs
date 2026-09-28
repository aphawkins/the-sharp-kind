// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;

namespace BubbleBobbleSharpLib.Players;

// $052A: a player who is not in the game presses fire and joins it, for a credit.
//
// The tail of $045C, reached through the jump at $049D. Player 2 is tried first. A joiner has four
// lives and is put in the dead state, so the next $045C takes one of them and respawns the player.
// The 6502 also blanks that player's lives cells ($0584-$05A5); the HUD is drawn from the lives, so
// it shows the four for one pass where the C64 shows none.
internal sealed class PlayerJoin
{
    private const byte JoinLives = 0x04;

    private readonly EntityTable _entities;
    private readonly PlayerTable _players;
    private readonly Scores _scores;

    internal PlayerJoin(EntityTable entities, PlayerTable players, Scores scores)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(scores);

        _entities = entities;
        _players = players;
        _scores = scores;
    }

    // $049D holds a jump to $052A until a join takes the credits below zero, which turns it into an rts
    // (a new game, $0997, puts the jump back). That join, and any later one in the same call, still goes ahead.
    private bool Closed => (sbyte)_players.Credits < 0;

    internal void Update(ReadOnlySpan<byte> ports)
    {
        if (Closed)
        {
            return;
        }

        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (_entities.State[player] == 0 && (ports[player] & Input.Fire) == 0)
            {
                Join(player);
            }
        }
    }

    private void Join(int player)
    {
        _players.Lives[player] = JoinLives;
        _entities.State[player] = PlayerFrame.DeadState;

        _scores.Clear(player);
        _players.ExtraLifeStep[player] = 0;
        _players.ExtraLifeByte[player] = (byte)(Scores.Last(player) - 1);

        _players.Credits--;
    }
}

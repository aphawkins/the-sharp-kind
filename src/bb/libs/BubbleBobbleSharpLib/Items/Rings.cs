// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Items;

internal sealed class Rings
{
    private const byte RingScore = 0x01;

    private readonly PlayerTable _players;
    private readonly Scores _scores;

    internal Rings(PlayerTable players, Scores scores)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(scores);

        _players = players;
        _scores = scores;
    }

    internal void Drift(int player) => Score(player, _players.DriftRing[player]);

    internal void Walk(int player) => Score(player, _players.WalkRing[player]);

    internal void Blow(int player) => Score(player, _players.BlowRing[player]);

    private void Score(int player, byte ring)
    {
        if (ring != 0)
        {
            _scores.Add(Scores.Last(player), RingScore);
        }
    }
}

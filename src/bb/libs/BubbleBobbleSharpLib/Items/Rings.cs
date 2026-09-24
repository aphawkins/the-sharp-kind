// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Items;

// The three rings' ten points. Each is one test and one call in the reference: while the ring's byte
// is not zero, $7C21 adds 1 to the player's lowest score byte - ten points, the score's last digit
// being a fixed nought.
//
//   * $2483, the drift ring, $61: a step sideways in the air, entered from the jump at $23FC. A fall
//     joins the drift at $248D, past the test, and scores nothing.
//   * $22B4, the walk ring, $63: a step along the ground that the wall let through.
//   * $23D7, the blow ring, $65: a bubble made.
internal sealed class Rings
{
    // $7C24's `lda #$01`.
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

    // $2483.
    internal void Drift(int player) => Score(player, _players.DriftRing[player]);

    // $22B4.
    internal void Walk(int player) => Score(player, _players.WalkRing[player]);

    // $23D7.
    internal void Blow(int player) => Score(player, _players.BlowRing[player]);

    // $7C21.
    private void Score(int player, byte ring)
    {
        if (ring != 0)
        {
            _scores.Add(Scores.Last(player), RingScore);
        }
    }
}

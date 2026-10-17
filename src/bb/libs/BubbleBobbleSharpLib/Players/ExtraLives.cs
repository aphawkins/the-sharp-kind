// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;

namespace BubbleBobbleSharpLib.Players;

// $F1AC, called at $0A10: a life for each mark a player's score passes.
//
// $AC and $AD (PlayerTable.ExtraLifeStep) count the marks passed and $AE and $AF (ExtraLifeByte) say which score byte
// to look at. The marks are $05B9's table, compared with the middle byte of the score (30,000, then 100,000 from the
// top byte, and on). The routine also asks for a sound ($F887, preset 3) when it awards; the port has no sound for
// that, only the songs.
internal sealed class ExtraLives
{
    // $05B9-$05C4.
    private static readonly byte[] s_marks = [0x30, 0x01, 0x04, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90];

    private readonly PlayerTable _players;
    private readonly Scores _scores;

    internal ExtraLives(PlayerTable players, Scores scores)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(scores);

        _players = players;
        _scores = scores;
    }

    internal void Update()
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (Earned(player))
            {
                Award(player);
            }
        }
    }

    private bool Earned(int player)
    {
        int index = _players.ExtraLifeByte[player] - 1;

        // $F1B0: the byte before the one named, unless that is off the score or is 2.
        if (index is >= 0 and not 2 && _scores.Bytes[index] != 0)
        {
            return true;
        }

        int step = _players.ExtraLifeStep[player];

        // Past the table the 6502 reads code from $05C5, whose smallest byte is above any score byte (BCD).
        return step < s_marks.Length && _scores.Bytes[index + 1] >= s_marks[step];
    }

    private void Award(int player)
    {
        _players.ExtraLifeStep[player]++;
        _players.Lives[player]++;

        // $F1D0: the first mark moves on to the score's next byte up.
        if (_players.ExtraLifeStep[player] == 1)
        {
            _players.ExtraLifeByte[player]--;
        }
    }
}

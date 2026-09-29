// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// $32C1-$3420 (`extend-bonus.s`): a player who has all six EXTEND letters (`$54`, `$55` at `$3F`) gets a life, loses the
// letters, and the level ends. Player 2 is looked at first. The game also stops for a fanfare and a screen of fruit, about
// five seconds of frames in which only the IRQ runs; none of that is modelled, so the level clock and the hurry-up do
// not run on through it as they do on the C64.
internal sealed class ExtendBonus
{
    private const byte AllLetters = 0x3F;

    private readonly PlayerTable _players;
    private readonly LevelEnd _end;

    internal ExtendBonus(PlayerTable players, LevelEnd end)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(end);

        _players = players;
        _end = end;
    }

    // How many times a player has earned the life, for the song ($32C1's fanfare).
    internal int Awards { get; private set; }

    internal void Update()
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (_players.ExtendLetters[player] != AllLetters)
            {
                continue;
            }

            _players.Lives[player]++;
            _players.ExtendLetters[player] = 0;
            _end.Skip();
            Awards++;
        }
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib;

// The songs the game asks for, numbered as the `song_table` of the reference's sid-wrapper.s numbers them. The enum's name is
// the key of the song in the asset manifest, which is where the HVSC bubble-bobble.sid differs: its song 7 is the EXTEND
// fanfare and its song 8 the title tune, the other way round.
internal enum Song
{
    Theme = 1,
    Resume = 2,
    FinalTheme = 3,
    BonusRound = 4,
    GameOver = 5,
    LevelComplete = 6,
    Title = 7,
    Extend = 8,
    HurryUp = 9,
    RoundStart = 10,
    SuperBonus = 11,
    Ending = 12,
}

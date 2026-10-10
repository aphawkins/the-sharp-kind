// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Screens;

// The screens of the game, and where the reference goes between them.
internal enum BbScreen
{
    // $4556: the logo and the credits, until fire is pressed.
    Title = 0,

    // $091B onwards: the game itself, until both players are out, and then the choice of players again.
    Play = 1,

    // $F005-$F0E3: the last game's scores, until 1 or 2 is pressed.
    Select = 2,

    // $08E4-$091B: the story, while the level's theme starts.
    Story = 3,
}

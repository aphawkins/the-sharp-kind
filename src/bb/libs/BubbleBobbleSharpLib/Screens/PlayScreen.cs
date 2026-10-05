// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Abstraction;

namespace BubbleBobbleSharpLib.Screens;

// $091B onwards: the game. Its update and its drawing are the game's own.
internal sealed class PlayScreen(Action update, Action draw) : IGameScreen
{
    public void Reset()
    {
    }

    public void Update() => update();

    public void Draw() => draw();
}

// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Audio;
using EliteSharpLib.Graphics;
using SharpKind.Audio;

namespace EliteSharpLib.Views;

internal sealed class HyperspaceView : IScreenController
{
    private readonly AudioController _audio;
    private readonly BreakPattern _breakPattern;
    private readonly GameState _gameState;

    internal HyperspaceView(GameState gameState, AudioController audio, IEliteDraw draw)
    {
        _gameState = gameState;
        _audio = audio;
        _breakPattern = new(draw);
    }

    // Nothing: this screen is the break pattern, which is drawn into the
    // universe layer below, and the HUD art draws the canopy over it.
    public void Draw()
    {
    }

    public void DrawUniverse() => _breakPattern.Draw();

    public void HandleInput()
    {
    }

    public void Reset()
    {
        _breakPattern.Reset();
        _audio.PlayEffect(nameof(SoundEffect.Hyperspace));
    }

    public void Update()
    {
        _breakPattern.Update(_gameState.Clock.Ticks);

        if (_breakPattern.IsComplete)
        {
            _gameState.SetView(Screen.FrontView);
        }
    }
}

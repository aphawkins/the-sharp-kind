// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharpLib.Audio;
using EliteSharpLib.Conflict;
using EliteSharpLib.Graphics;
using SharpKind.Audio;

namespace EliteSharpLib.Views;

internal sealed class LaunchView : IScreenController
{
    private readonly AudioController _audio;
    private readonly IBaseView _baseView;
    private readonly BreakPattern _breakPattern;
    private readonly Combat _combat;
    private readonly GameState _gameState;
    private readonly Space _space;
    private readonly Universe _universe;

    internal LaunchView(
        GameState gameState,
        AudioController audio,
        Space space,
        Combat combat,
        Universe universe,
        IEliteDraw draw,
        IBaseView baseView)
    {
        _gameState = gameState;
        _audio = audio;
        _space = space;
        _combat = combat;
        _universe = universe;
        _baseView = baseView;
        _breakPattern = new(draw);
    }

    public void Draw() => _baseView.DrawBorder();

    public void DrawUniverse() => _breakPattern.Draw();

    public void HandleInput()
    {
    }

    public void Reset()
    {
        _combat.Reset();
        _universe.ClearUniverse();
        _breakPattern.Reset();
        _audio.PlayEffect(nameof(SoundEffect.Launch));
    }

    public void Update()
    {
        _breakPattern.Update(_gameState.Clock.Ticks);

        if (_breakPattern.IsComplete)
        {
            _space.LaunchPlayer();
            _gameState.SetView(Screen.FrontView);
        }
    }
}

// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharpLib.Save;
using SharpKind.Input;

namespace EliteSharpLib.Views;

/// <summary>
/// The load-commander screen's behaviour: typing a name, then either loading
/// it and moving on, or reporting the failure.
/// </summary>
internal sealed class LoadCommanderController : IScreenController
{
    private readonly GameState _gameState;
    private readonly IKeyboard _keyboard;
    private readonly SaveFile _save;
    private readonly IView<LoadCommanderModel> _view;

    private bool _isLoaded = true;
    private string _name = string.Empty;

    internal LoadCommanderController(GameState gameState, IKeyboard keyboard, SaveFile save, IView<LoadCommanderModel> view)
    {
        _gameState = gameState;
        _keyboard = keyboard;
        _save = save;
        _view = view;
    }

    public void Draw() => _view.Draw(BuildModel());

    public void HandleInput()
    {
        if (_keyboard.IsPressed(ConsoleKey.Backspace) &&
            !string.IsNullOrEmpty(_name))
        {
            _name = _name[..^1];
        }

        (ConsoleKey key, ConsoleModifiers _) = _keyboard.LastPressed();
        if (key is >= ConsoleKey.A and <= ConsoleKey.Z)
        {
            _name += (char)key;
        }

        if (_keyboard.IsPressed(ConsoleKey.Enter))
        {
            _isLoaded = _save.LoadCommander(_name);
            if (_isLoaded)
            {
                _save.GetLastSave();
                _gameState.SetView(Screen.CommanderStatus);
            }
        }

        if (_keyboard.IsPressed(ConsoleKey.Spacebar))
        {
            _gameState.SetView(Screen.CommanderStatus);
        }
    }

    public void Reset()
    {
        _keyboard.ClearPressed();
        _name = _gameState.Cmdr.Name;
        _isLoaded = true;
    }

    public void Update()
    {
    }

    // Exposed for tests: the typed name and why the last attempt failed. The reason comes from
    // the save file, e.g. "Bad Fuel", so a commander can act on it rather than a generic error.
    internal LoadCommanderModel BuildModel() => new(_name, _isLoaded ? string.Empty : _save.LastLoadError);
}

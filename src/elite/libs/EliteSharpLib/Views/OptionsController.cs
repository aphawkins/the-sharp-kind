// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using SharpKind.Input;

namespace EliteSharpLib.Views;

/// <summary>
/// The options menu's behaviour: the cursor over its rows, and choosing one.
/// The docked-only rows are greyed out while undocked rather than hidden.
/// <para>
/// Back is the last row rather than the first, so the rows read as the things
/// the commander came here to do with the way out under them - the same place
/// the settings screens put theirs. Where it goes back to is the screen the
/// commander opened the options from, which <see cref="GameState"/> keeps: F11
/// works from the title screens too, and those had no way out before.
/// </para>
/// </summary>
internal sealed class OptionsController : IScreenController
{
    private readonly (string Label, bool DockedOnly)[] _optionList =
    [
        new("Save Commander", true),
        new("Load Commander", true),
        new("Game Settings", false),
        new("Engine Settings", false),
        new("Credits", false),
        new("Quit", false),
        new("Back", false),
    ];

    private readonly GameState _gameState;
    private readonly IKeyboard _keyboard;
    private readonly IView<OptionsModel> _view;

    private int _highlightedItem;

    internal OptionsController(GameState gameState, IKeyboard keyboard, IView<OptionsModel> view)
    {
        _gameState = gameState;
        _keyboard = keyboard;
        _view = view;
    }

    public void Draw() => _view.Draw(BuildModel());

    public void HandleInput()
    {
        // The cursor wraps: Back is last, so up from the top reaches it in one press.
        if (_keyboard.IsPressed(ConsoleKey.S) || _keyboard.IsPressed(ConsoleKey.UpArrow))
        {
            _highlightedItem = (_highlightedItem + _optionList.Length - 1) % _optionList.Length;
        }

        if (_keyboard.IsPressed(ConsoleKey.X) || _keyboard.IsPressed(ConsoleKey.DownArrow))
        {
            _highlightedItem = (_highlightedItem + 1) % _optionList.Length;
        }

        if (_keyboard.IsPressed(ConsoleKey.Enter))
        {
            ExecuteOption();
        }
    }

    public void Reset() => _highlightedItem = 0;

    public void Update()
    {
    }

    // Exposed for tests: the rows' enabled state and the cursor position.
    internal OptionsModel BuildModel()
    {
        OptionRow[] rows = new OptionRow[_optionList.Length];
        for (int i = 0; i < _optionList.Length; i++)
        {
            rows[i] = new(_optionList[i].Label, _gameState.IsDocked || !_optionList[i].DockedOnly);
        }

        return new(rows, _highlightedItem);
    }

    private void ExecuteOption()
    {
        if (_gameState.IsDocked || !_optionList[_highlightedItem].DockedOnly)
        {
            switch (_highlightedItem)
            {
                case 0:
                    _gameState.SetView(Screen.SaveCommander);
                    break;

                case 1:
                    _gameState.SetView(Screen.LoadCommander);
                    break;

                case 2:
                    _gameState.SetView(Screen.Settings);
                    break;

                case 3:
                    _gameState.SetView(Screen.EngineSettings);
                    break;

                case 4:
                    _gameState.SetView(Screen.Credits);
                    break;

                case 5:
                    _gameState.SetView(Screen.Quit);
                    break;

                case 6:
                    _gameState.SetView(_gameState.OptionsReturn);
                    break;
            }
        }
    }
}

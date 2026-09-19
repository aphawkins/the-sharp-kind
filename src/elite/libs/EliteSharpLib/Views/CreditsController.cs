// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using SharpKind.Input;

namespace EliteSharpLib.Views;

/// <summary>
/// The credits screen: who wrote the game, and which build this is. It used to
/// be a footer under the options rows, which left the version competing with
/// the menu for the screen; on its own it can say the whole of each name.
/// <para>
/// There is one thing to do here - go back - so both Enter and Escape do it,
/// and nothing moves a cursor that has only one row to sit on.
/// </para>
/// </summary>
internal sealed class CreditsController : IScreenController
{
    private static readonly IReadOnlyList<string> s_credits =
    [
        "The Sharp Kind - A Hawkins",
        "The New Kind - C Pinder",
        "Original Game - I Bell & D Braben",
        "Music - D Dunn & A Bell",
    ];

    private readonly GameState _gameState;
    private readonly IKeyboard _keyboard;
    private readonly IView<CreditsModel> _view;

    internal CreditsController(GameState gameState, IKeyboard keyboard, IView<CreditsModel> view)
    {
        _gameState = gameState;
        _keyboard = keyboard;
        _view = view;
    }

    public void Draw() => _view.Draw(BuildModel());

    public void HandleInput()
    {
        if (_keyboard.IsPressed(ConsoleKey.Enter) || _keyboard.IsPressed(ConsoleKey.Escape))
        {
            _gameState.SetView(Screen.Options);
        }
    }

    public void Reset()
    {
    }

    public void Update()
    {
    }

    // Exposed for tests: the version and the names shown.
    internal static CreditsModel BuildModel() => new(
        $"Version: {typeof(CreditsController).Assembly.GetName().Version}",
        s_credits);
}

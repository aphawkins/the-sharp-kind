// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharpLib.Fakes;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests.Views;

// A docked-only row is greyed out, not hidden, while undocked, so the model carries that as a per-row flag rather than a shorter list.
public class OptionsControllerTests
{
    [Fact]
    public void DockedOnlyRowsAreEnabledWhileDocked()
    {
        OptionsController controller = CreateController(out GameState gameState);
        gameState.IsDocked = true;

        Assert.All(controller.BuildModel().Options, row => Assert.True(row.IsEnabled));
    }

    [Fact]
    public void DockedOnlyRowsAreDisabledWhileUndocked()
    {
        OptionsController controller = CreateController(out GameState gameState);
        gameState.IsDocked = false;

        OptionsModel model = controller.BuildModel();

        Assert.False(model.Options[0].IsEnabled); // Save Commander
        Assert.False(model.Options[1].IsEnabled); // Load Commander
        Assert.True(model.Options[2].IsEnabled); // Game Settings
    }

    [Fact]
    public void ResetPutsTheCursorOnTheFirstRow()
    {
        OptionsController controller = CreateController(out _);

        controller.Reset();

        Assert.Equal(0, controller.BuildModel().HighlightedIndex);
    }

    // Back is the last row, so wrapping upwards off the top is the shortest way
    // to it.
    [Fact]
    public void TheCursorWrapsFromTheTopRowToTheBottom()
    {
        OptionsController controller = CreateController(out _, out FakeKeyboard keyboard);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.UpArrow, default);
        controller.HandleInput();

        OptionsModel model = controller.BuildModel();

        Assert.Equal(model.Options.Count - 1, model.HighlightedIndex);
    }

    [Fact]
    public void TheCursorWrapsFromTheBottomRowToTheTop()
    {
        OptionsController controller = CreateController(out _, out FakeKeyboard keyboard);
        controller.Reset();

        for (int i = 0; i < controller.BuildModel().Options.Count; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
            keyboard.KeyUp(ConsoleKey.DownArrow, default);
        }

        Assert.Equal(0, controller.BuildModel().HighlightedIndex);
    }

    // Options are reachable from the title screens, so Back must return to whichever screen opened them, not a fixed one.
    [Fact]
    public void BackReturnsToTheScreenTheOptionsWereOpenedFrom()
    {
        OptionsController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard);
        gameState.SetView(Screen.IntroTwo);
        gameState.EnterOptions();
        controller.Reset();

        SelectBackRow(controller, keyboard);

        Assert.Equal(Screen.IntroTwo, gameState.CurrentScreen);
    }

    // A settings screen returning here uses a plain SetView, so the way out
    // still leads all the way back rather than to the screen just left.
    [Fact]
    public void ReturningFromASettingsScreenLeavesTheWayOutWhereItWas()
    {
        OptionsController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard);
        gameState.SetView(Screen.IntroTwo);
        gameState.EnterOptions();
        gameState.SetView(Screen.EngineSettings);
        gameState.SetView(Screen.Options);
        controller.Reset();

        SelectBackRow(controller, keyboard);

        Assert.Equal(Screen.IntroTwo, gameState.CurrentScreen);
    }

    // The key that opens options closes them again, rather than reopening them over themselves.
    [Fact]
    public void OpeningTheOptionsAgainClosesThem()
    {
        CreateController(out GameState gameState, out _);
        gameState.SetView(Screen.IntroTwo);
        gameState.EnterOptions();

        gameState.EnterOptions();

        Assert.Equal(Screen.IntroTwo, gameState.CurrentScreen);
    }

    // Same trap one screen further in: the way out has to survive the key
    // being pressed on a screen the options themselves led to.
    [Fact]
    public void OpeningTheOptionsFromASettingsScreenLeavesTheWayOutWhereItWas()
    {
        OptionsController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard);
        gameState.SetView(Screen.IntroTwo);
        gameState.EnterOptions();
        gameState.SetView(Screen.EngineSettings);

        gameState.EnterOptions();
        controller.Reset();
        SelectBackRow(controller, keyboard);

        Assert.Equal(Screen.IntroTwo, gameState.CurrentScreen);
    }

    [Fact]
    public void TheCreditsRowOpensTheCreditsScreen()
    {
        OptionsController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard);
        controller.Reset();

        SelectRow(controller, keyboard, 4);

        Assert.Equal(Screen.Credits, gameState.CurrentScreen);
    }

    private static void SelectBackRow(OptionsController controller, FakeKeyboard keyboard)
        => SelectRow(controller, keyboard, controller.BuildModel().Options.Count - 1);

    private static void SelectRow(OptionsController controller, FakeKeyboard keyboard, int row)
    {
        for (int i = 0; i < row; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
        }

        keyboard.KeyUp(ConsoleKey.DownArrow, default);
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();
    }

    private static OptionsController CreateController(out GameState gameState)
        => CreateController(out gameState, out _);

    private static OptionsController CreateController(out GameState gameState, out FakeKeyboard keyboard)
    {
        keyboard = new FakeKeyboard();
        ScreenManager<Screen, IScreenController> views = new(keyboard);
        views.Add(Screen.Options, new FakeView());
        views.Add(Screen.Credits, new FakeView());
        views.Add(Screen.EngineSettings, new FakeView());
        views.Add(Screen.IntroTwo, new FakeView());
        gameState = new(views, TestMissions.Registry());

        return new OptionsController(gameState, keyboard, new FakeOptionsView());
    }

    private sealed class FakeOptionsView : IView<OptionsModel>
    {
        public void Draw(OptionsModel model)
        {
            // Drawing is not under test here.
        }
    }
}

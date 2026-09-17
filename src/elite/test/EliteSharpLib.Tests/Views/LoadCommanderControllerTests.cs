// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharpLib.Fakes;
using EliteSharpLib.Save;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests.Views;

// The load-commander screen's behaviour, with no renderer involved: typing,
// then either an error or an immediate move to CommanderStatus.
public class LoadCommanderControllerTests
{
    [Fact]
    public void ResetStartsWithTheCurrentCommandersName()
    {
        LoadCommanderController controller = CreateController(out GameState gameState, out _, out _);
        gameState.Cmdr.Name = "Max";

        controller.Reset();

        Assert.Equal("Max", controller.BuildModel().Name);
    }

    [Fact]
    public void TypingLettersAndBackspaceEditsTheName()
    {
        LoadCommanderController controller = CreateController(out _, out FakeKeyboard keyboard, out _);
        controller.Reset();

        Type(controller, keyboard, "MAX");
        Assert.Equal("MAX", controller.BuildModel().Name);

        keyboard.KeyDown(ConsoleKey.Backspace, default);
        controller.HandleInput();
        Assert.Equal("MA", controller.BuildModel().Name);
    }

    [Fact]
    public void LoadingAMissingCommanderShowsAnErrorAndStaysPut()
    {
        // Typed input is always upper-case: A-Z is the only range HandleInput accepts letters from.
        LoadCommanderController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard, out _);
        controller.Reset();

        Type(controller, keyboard, "NOSUCHCOMMANDER");
        PressEnter(controller, keyboard);

        Assert.NotEqual(string.Empty, controller.BuildModel().ErrorMessage);
        Assert.NotEqual(Screen.CommanderStatus, gameState.CurrentScreen);
    }

    [Fact]
    public void LoadingASavedCommanderClearsTheErrorAndMovesOn()
    {
        LoadCommanderController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard, out SaveFile save);

        // A save is only loadable if written from a real commander; clear the name it would prefill with.
        save.GetLastSave();
        save.SaveCommander("MAX");
        gameState.Cmdr.Name = string.Empty;
        controller.Reset();

        Type(controller, keyboard, "MAX");
        PressEnter(controller, keyboard);

        Assert.Equal(string.Empty, controller.BuildModel().ErrorMessage);
        Assert.Equal(Screen.CommanderStatus, gameState.CurrentScreen);
    }

    [Fact]
    public void SpaceAlwaysReturnsToCommanderStatus()
    {
        LoadCommanderController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard, out _);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.Spacebar, default);
        controller.HandleInput();

        Assert.Equal(Screen.CommanderStatus, gameState.CurrentScreen);
    }

    private static void Type(LoadCommanderController controller, FakeKeyboard keyboard, string text)
    {
        foreach (char c in text)
        {
            keyboard.KeyDown((ConsoleKey)c, default);
            controller.HandleInput();
            keyboard.KeyUp((ConsoleKey)c, default);
        }
    }

    private static void PressEnter(LoadCommanderController controller, FakeKeyboard keyboard)
    {
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();
        keyboard.KeyUp(ConsoleKey.Enter, default);
    }

    private static LoadCommanderController CreateController(
        out GameState gameState, out FakeKeyboard keyboard, out SaveFile save)
    {
        keyboard = new FakeKeyboard();
        ScreenManager<Screen, IScreenController> views = new(keyboard);
        views.Add(Screen.CommanderStatus, new FakeView());
        gameState = new(views, TestMissions.Registry());
        PlayerShip ship = new(gameState);
        Trade trade = TestGoods.Trade(gameState, ship);
        string directory = Path.Combine(Path.GetTempPath(), "LoadCommanderControllerTests_" + Guid.NewGuid().ToString("N"));
        save = new SaveFile(gameState, ship, trade, new PlanetController(gameState), TestMissions.Registry(), directory);

        return new LoadCommanderController(gameState, keyboard, save, new FakeLoadCommanderView());
    }

    private sealed class FakeLoadCommanderView : IView<LoadCommanderModel>
    {
        public void Draw(LoadCommanderModel model)
        {
            // Drawing is not under test here.
        }
    }
}

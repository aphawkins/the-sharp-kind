// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Config;
using EliteSharpLib.Fakes;
using EliteSharpLib.Planets;
using EliteSharpLib.Views;
using SharpKind.Config;
using SharpKind.Fakes.Input;
using SharpKind.Graphics.Rendering;

namespace EliteSharpLib.Tests.Views;

public class SettingsControllerTests
{
    private const string ConfigFileName = "elite.sharp";

    [Fact]
    public void ChangingASettingSavesItImmediately()
    {
        // Arrange: the screen has no save step, so the very first toggle must
        // already be on disk.
        SettingsController controller = CreateController(
            out GameState gameState, out FakeKeyboard keyboard, out ConfigFile<EliteConfig> configFile);
        keyboard.KeyDown(ConsoleKey.Enter, default);

        // Act: item 0 is Planet Style, which starts at Fractal and wraps.
        controller.HandleInput();

        // Assert
        Assert.Equal(PlanetType.Solid, gameState.Config.Game.PlanetStyle);
        Assert.Equal(PlanetType.Solid, configFile.ReadConfig().Game.PlanetStyle);
    }

    // The engine's settings belong to the other screen.
    [Fact]
    public void ChangingAGameSettingLeavesTheEngineSettingsAlone()
    {
        SettingsController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard, out _);
        keyboard.KeyDown(ConsoleKey.Enter, default);

        controller.HandleInput();

        Assert.Equal(FillMode.Solid, gameState.Config.Engine.Graphics.FillMode);
    }

    [Fact]
    public void BackReturnsToOptionsWithoutChangingSettings()
    {
        SettingsController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard, out _);
        controller.Reset();

        // Navigate to the last row - the Back row.
        for (int i = 0; i < 4; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
        }

        keyboard.KeyUp(ConsoleKey.DownArrow, default);
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal(Screen.Options, gameState.CurrentScreen);
        Assert.Equal(PlanetType.Fractal, gameState.Config.Game.PlanetStyle);
    }

    // The cursor wraps as on the options menu; Back is the last row, so one press up from the first setting is the way out.
    [Fact]
    public void TheCursorWrapsUpFromTheFirstSettingToBack()
    {
        SettingsController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard, out _);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.UpArrow, default);
        controller.HandleInput();
        keyboard.KeyUp(ConsoleKey.UpArrow, default);
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal(Screen.Options, gameState.CurrentScreen);
        Assert.Equal(PlanetType.Fractal, gameState.Config.Game.PlanetStyle);
    }

    [Fact]
    public void TheCursorWrapsDownFromBackToTheFirstSetting()
    {
        SettingsController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard, out _);
        controller.Reset();

        // Past the Back row, which is one further down than there are settings.
        for (int i = 0; i <= controller.Settings.Count; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
            keyboard.KeyUp(ConsoleKey.DownArrow, default);
        }

        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal(PlanetType.Solid, gameState.Config.Game.PlanetStyle);
    }

    private static SettingsController CreateController(
        out GameState gameState,
        out FakeKeyboard keyboard,
        out ConfigFile<EliteConfig> configFile)
    {
        Space space = SettingsControllerFixture.CreateSpace(out gameState, out keyboard, out FakeEliteDraw draw, out _);
        configFile = SettingsControllerFixture.CreateConfigFile(ConfigFileName);

        return new SettingsController(
            gameState,
            keyboard,
            space,
            configFile,
            SettingsControllerFixture.CreateBaseView(draw),
            draw,
            SettingsControllerFixture.CreateStyle(draw));
    }
}

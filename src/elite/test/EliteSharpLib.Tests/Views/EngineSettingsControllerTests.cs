// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Renditions.EightBit;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Config;
using EliteSharpLib.Fakes;
using EliteSharpLib.Planets;
using EliteSharpLib.Renditions;
using EliteSharpLib.Views;
using SharpKind.Audio;
using SharpKind.Config;
using SharpKind.Fakes.Input;
using SharpKind.Graphics;
using SharpKind.Graphics.Rendering;

namespace EliteSharpLib.Tests.Views;

public class EngineSettingsControllerTests
{
    private const string ConfigFileName = "elite.sharp";

    [Fact]
    public void ChangingASettingSavesItImmediately()
    {
        // Arrange: as the game settings screen, there's no save step.
        EngineSettingsController controller = CreateController(
            out GameState gameState, out FakeKeyboard keyboard, out _, out ConfigFile<EliteConfig> configFile);
        keyboard.KeyDown(ConsoleKey.Enter, default);

        // Act: item 0 is Fill Mode.
        controller.HandleInput();

        // Assert
        Assert.Equal(FillMode.Wireframe, gameState.Config.Engine.Graphics.FillMode);
        Assert.Equal(FillMode.Wireframe, configFile.ReadConfig().Engine.Graphics.FillMode);
    }

    // Row 2 is Shading, which defaults Unlit - the flat original is what ships
    // - so stepping it once selects Lambert and saves that.
    [Fact]
    public void SelectingLambertShadingSavesIt()
    {
        EngineSettingsController controller = CreateController(
            out GameState gameState, out FakeKeyboard keyboard, out _, out ConfigFile<EliteConfig> configFile);
        controller.Reset();

        for (int i = 0; i < 2; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
        }

        keyboard.KeyUp(ConsoleKey.DownArrow, default);
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal(ShadingModelKind.Lambert, gameState.Config.Engine.Graphics.Shading);
        Assert.Equal(ShadingModelKind.Lambert, configFile.ReadConfig().Engine.Graphics.Shading);
    }

    // Row 3 is Quantisation, added alongside Shading as the pipeline's output
    // stage - the two are separate choices, so both get their own row.
    [Fact]
    public void SelectingOrderedQuantisationSavesIt()
    {
        EngineSettingsController controller = CreateController(
            out GameState gameState, out FakeKeyboard keyboard, out _, out ConfigFile<EliteConfig> configFile);
        controller.Reset();

        for (int i = 0; i < 3; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
        }

        keyboard.KeyUp(ConsoleKey.DownArrow, default);
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal(Quantisation.Ordered, gameState.Config.Engine.Graphics.Quantisation);
        Assert.Equal(Quantisation.Ordered, configFile.ReadConfig().Engine.Graphics.Quantisation);
    }

    // Row 4 is Font. It has no restart marker, so choosing a kind has to
    // reach the running renderer as well as the file - the same bargain the
    // Music row below makes with the audio controller.
    [Fact]
    public void SelectingAFontKindAppliesItToTheRunningRenderer()
    {
        EngineSettingsController controller = CreateController(
            out GameState gameState,
            out FakeKeyboard keyboard,
            out _,
            out ConfigFile<EliteConfig> configFile,
            out FakeEliteDraw draw);
        controller.Reset();

        for (int i = 0; i < 4; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
        }

        Assert.Equal(["Bitmap", "FON", "TrueType"], controller.Settings[4].Values);

        keyboard.KeyUp(ConsoleKey.DownArrow, default);
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal(FontKind.Fon, gameState.Config.Engine.Graphics.FontKind);
        Assert.Equal(FontKind.Fon, configFile.ReadConfig().Engine.Graphics.FontKind);
        Assert.Equal(FontKind.Fon, draw.Graphics.FontKind);
    }

    // Row 5 is Music: the config and the running AudioController have to move
    // together, or the setting only takes effect after a restart.
    [Fact]
    public void TurningMusicOffAppliesToTheRunningAudioController()
    {
        EngineSettingsController controller = CreateController(
            out GameState gameState, out FakeKeyboard keyboard, out AudioController audio, out _);
        controller.Reset();

        // Down five times to the Music row, then toggle.
        for (int i = 0; i < 5; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
        }

        keyboard.KeyUp(ConsoleKey.DownArrow, default);
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.False(gameState.Config.Engine.Sound.Music);
        Assert.False(audio.MusicOn);
    }

    // Row 8 is Window Scale. The 16-bit tier this fixture builds offers 1 and
    // 2, so stepping off its default of 2 lands on 1 - and the row shows the
    // scales as multipliers rather than bare numbers.
    [Fact]
    public void SelectingAWindowScaleSavesIt()
    {
        EngineSettingsController controller = CreateController(
            out GameState gameState, out FakeKeyboard keyboard, out _, out ConfigFile<EliteConfig> configFile);
        controller.Reset();

        for (int i = 0; i < 8; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
        }

        Assert.Equal(["1x", "2x"], controller.Settings[8].Values);

        keyboard.KeyUp(ConsoleKey.DownArrow, default);
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal(1, gameState.Config.Engine.WindowScale);
        Assert.Equal(1, configFile.ReadConfig().Engine.WindowScale);
    }

    // Row 9 is Field of View. Unchosen, it reads back the original's own
    // 53 degrees, and selecting that value stores nothing so the classic
    // projection stays exact. Stepping the row on once moves to 65.
    [Fact]
    public void SelectingAFieldOfViewSavesIt()
    {
        EngineSettingsController controller = CreateController(
            out GameState gameState, out FakeKeyboard keyboard, out _, out ConfigFile<EliteConfig> configFile);
        controller.Reset();

        Assert.Null(gameState.Config.Engine.FieldOfView);
        Assert.Equal(["53", "65", "75", "90", "105"], controller.Settings[9].Values);
        Assert.Equal(0, controller.Settings[9].SelectedIndex);

        for (int i = 0; i < 9; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
        }

        keyboard.KeyUp(ConsoleKey.DownArrow, default);
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal(65, gameState.Config.Engine.FieldOfView);
        Assert.Equal(65, configFile.ReadConfig().Engine.FieldOfView);
    }

    // Row 10 is Rendition - Design Scale was inserted at 9, between it and
    // Window Scale. Switching it to the 8-bit tier does not reload
    // anything - that only happens on restart - but the Window Scale row
    // above it has to offer that tier's own scales straight away: showing
    // the 16-bit tier's 1 and 2 while the config underneath already says
    // 8-bit would let a commander pick a scale the tier they are about to
    // restart into does not offer, or hide 4, which it does.
    [Fact]
    public void SwitchingRenditionUpdatesTheWindowScalesOffered()
    {
        EngineSettingsController controller = CreateController(
            out _, out FakeKeyboard keyboard, out _, out _);
        controller.Reset();

        int scaleRow = RowNamed(controller, "Window Scale");
        Assert.Equal(["1x", "2x"], controller.Settings[scaleRow].Values);

        MoveTo(controller, keyboard, RowNamed(controller, "Rendition"));
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal(["1x", "2x", "4x"], controller.Settings[scaleRow].Values);
    }

    // The game's own settings belong to the other screen.
    [Fact]
    public void ChangingAnEngineSettingLeavesTheGameSettingsAlone()
    {
        EngineSettingsController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard, out _, out _);
        keyboard.KeyDown(ConsoleKey.Enter, default);

        controller.HandleInput();

        Assert.Equal(PlanetType.Fractal, gameState.Config.Game.PlanetStyle);
    }

    [Fact]
    public void BackReturnsToOptionsWithoutChangingSettings()
    {
        EngineSettingsController controller = CreateController(out GameState gameState, out FakeKeyboard keyboard, out _, out _);
        controller.Reset();

        // The Back row sits one past the settings themselves.
        MoveTo(controller, keyboard, controller.Settings.Count);
        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal(Screen.Options, gameState.CurrentScreen);
        Assert.Equal(FillMode.Solid, gameState.Config.Engine.Graphics.FillMode);
    }

    private static EngineSettingsController CreateController(
        out GameState gameState,
        out FakeKeyboard keyboard,
        out AudioController audio,
        out ConfigFile<EliteConfig> configFile)
        => CreateController(out gameState, out keyboard, out audio, out configFile, out _);

    private static EngineSettingsController CreateController(
        out GameState gameState,
        out FakeKeyboard keyboard,
        out AudioController audio,
        out ConfigFile<EliteConfig> configFile,
        out FakeEliteDraw draw)
    {
        Space space = SettingsControllerFixture.CreateSpace(out gameState, out keyboard, out draw, out audio);
        configFile = SettingsControllerFixture.CreateConfigFile(ConfigFileName);

        // The fixture's draw surface, base view and style are all built for
        // the 16-bit tier (see SettingsControllerFixture), so the config has
        // to say the same - Elite's own default is 8-bit, which would leave
        // the Window Scale row reading a tier other than the one everything
        // else here is built for.
        gameState.Config.Engine.Rendition = "16-bit";

        return new EngineSettingsController(
            gameState,
            keyboard,
            space,
            audio,
            configFile,
            new InstalledRenditions(
                new SixteenBitRendition(),
                string.Empty,
                [new EightBitRendition(), new SixteenBitRendition()]),
            new FakeGamepad(),
            SettingsControllerFixture.CreateBaseView(draw),
            draw,
            SettingsControllerFixture.CreateStyle(draw));
    }

    // Rows are found by name and counted at runtime: these tests used fixed
    // indices and step counts, and every new setting silently moved them.
    private static int RowNamed(EngineSettingsController controller, string name)
    {
        for (int i = 0; i < controller.Settings.Count; i++)
        {
            if (controller.Settings[i].Name.StartsWith(name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"No settings row named '{name}'.");
    }

    private static void MoveTo(EngineSettingsController controller, FakeKeyboard keyboard, int row)
    {
        for (int i = 0; i < row; i++)
        {
            keyboard.KeyDown(ConsoleKey.DownArrow, default);
            controller.HandleInput();
        }

        keyboard.KeyUp(ConsoleKey.DownArrow, default);
    }
}

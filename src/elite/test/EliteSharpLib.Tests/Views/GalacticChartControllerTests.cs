// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests.Views;

// The controller works in galaxy space and hands the view a model, so none of this needs a draw surface.
public class GalacticChartControllerTests
{
    [Fact]
    public void ResetPlotsAllTwoHundredAndFiftySixStars()
    {
        GalacticChartController controller = CreateController(out _, out _);

        controller.Reset();

        Assert.Equal(256, controller.BuildModel().Stars.Count);
    }

    [Fact]
    public void ResetPlotsStarsInGalaxySpace()
    {
        GalacticChartController controller = CreateController(out _, out _);

        controller.Reset();

        // Galaxy space is the raw (D, B) of each seed: every star must land inside 0-255 on both axes.
        Assert.All(
            controller.BuildModel().Stars,
            star =>
            {
                Assert.InRange(star.Position.X, 0, 255);
                Assert.InRange(star.Position.Y, 0, 255);
            });
    }

    [Theory]
    [InlineData(ConsoleKey.LeftArrow, -1, 0)]
    [InlineData(ConsoleKey.RightArrow, 1, 0)]
    [InlineData(ConsoleKey.UpArrow, 0, -2)]
    [InlineData(ConsoleKey.DownArrow, 0, 2)]
    public void MoveCrossStepsOneGalaxyUnitPerAxis(ConsoleKey key, float dx, float dy)
    {
        GalacticChartController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();
        MoveToInterior(controller, keyboard);
        Vector2 before = controller.Cross;

        keyboard.KeyDown(key, default);
        controller.HandleInput();

        Assert.Equal(before + new Vector2(dx, dy), controller.Cross);
    }

    [Fact]
    public void MoveCrossClampsToTheGalaxyBounds()
    {
        GalacticChartController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();

        // Far more presses than the 0-255 span, so the clamp is what stops it.
        keyboard.KeyDown(ConsoleKey.LeftArrow, default);
        for (int i = 0; i < 400; i++)
        {
            controller.HandleInput();
        }

        Assert.Equal(0.5f, controller.Cross.X);

        keyboard.ClearPressed();
        keyboard.KeyDown(ConsoleKey.UpArrow, default);
        for (int i = 0; i < 400; i++)
        {
            controller.HandleInput();
        }

        Assert.Equal(0, controller.Cross.Y);
    }

    [Fact]
    public void OriginKeyPutsTheCrossOnTheDockedPlanet()
    {
        GalacticChartController controller = CreateController(out FakeKeyboard keyboard, out GameState gameState);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.RightArrow, default);
        controller.HandleInput();
        keyboard.ClearPressed();

        keyboard.KeyDown(ConsoleKey.O, default);
        controller.HandleInput();

        Assert.Equal(new(gameState.DockedPlanet.D, gameState.DockedPlanet.B), controller.Cross);
    }

    [Fact]
    public void FindPromptCapturesTypedNameAndIsShownAsTheCaption()
    {
        GalacticChartController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.F, default);
        controller.HandleInput();

        Assert.Equal("Planet Name?", controller.BuildModel().Caption);

        keyboard.ClearPressed();
        keyboard.KeyDown(ConsoleKey.L, default);
        controller.HandleInput();
        keyboard.ClearPressed();
        keyboard.KeyDown(ConsoleKey.A, default);
        controller.HandleInput();

        Assert.Equal("LA", controller.BuildModel().Detail);
    }

    [Fact]
    public void FindBackspaceRemovesTheLastCharacter()
    {
        GalacticChartController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.F, default);
        controller.HandleInput();
        keyboard.ClearPressed();
        keyboard.KeyDown(ConsoleKey.L, default);
        controller.HandleInput();
        keyboard.ClearPressed();

        keyboard.KeyDown(ConsoleKey.Backspace, default);
        controller.HandleInput();

        Assert.Equal(string.Empty, controller.BuildModel().Detail);
    }

    [Fact]
    public void UnknownPlanetIsCaptionedWhenNoPlanetIsNamed()
    {
        GalacticChartController controller = CreateController(out _, out GameState gameState);
        controller.Reset();
        gameState.PlanetName = string.Empty;

        Assert.Equal("Unknown Planet", controller.BuildModel().Caption);
    }

    [Fact]
    public void DistanceIsOmittedWhenTheCrossIsOnTheDockedPlanet()
    {
        GalacticChartController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.O, default);
        controller.HandleInput();

        // Zero distance prints nothing rather than "Distance: 0.0".
        Assert.Equal(string.Empty, controller.BuildModel().Detail);
    }

    // The default commander starts on a galaxy edge, where the clamp would
    // mask a step; nudge well inside it first.
    private static void MoveToInterior(GalacticChartController controller, FakeKeyboard keyboard)
    {
        keyboard.KeyDown(ConsoleKey.RightArrow, default);
        keyboard.KeyDown(ConsoleKey.DownArrow, default);
        for (int i = 0; i < 20; i++)
        {
            controller.HandleInput();
        }

        keyboard.ClearPressed();
    }

    private static GalacticChartController CreateController(out FakeKeyboard keyboard, out GameState gameState)
    {
        keyboard = new FakeKeyboard();
        ScreenManager<Screen, IScreenController> views = new(keyboard);
        gameState = new(views, TestMissions.Registry());

        return new GalacticChartController(
            gameState,
            keyboard,
            new PlanetController(gameState),
            new PlayerShip(gameState),
            new FakeGalacticChartView());
    }

    private sealed class FakeGalacticChartView : IView<GalacticChartModel>
    {
        public void Draw(GalacticChartModel model)
        {
            // Drawing is not under test here.
        }
    }
}

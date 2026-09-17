// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Fakes;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Types;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests.Views;

// Unlike every other controller, this one works in screen space: the tier's layout, not the
// galaxy, decides where the cross-hair may go, so most assertions are about that boundary.
public class ShortRangeChartControllerTests
{
    // 16-bit tier: 512x512 screen, 384 viewport height, so the cross-hair's box is x 1..510, y 37..351.
    private const float MinX = 1;
    private const float MaxX = 510;
    private const float MinY = 37;
    private const float MaxY = 351;

    [Fact]
    public void ResetPlotsOnlyThePlanetsInsideTheChartsWindow()
    {
        ShortRangeChartController controller = CreateController(out _, out _);

        controller.Reset();

        ShortRangeChartModel model = controller.BuildModel();
        Assert.NotEmpty(model.Planets);

        // Window is 20 galaxy units either side on D and 38 on B, at 4 and 2 px/unit and scale 2: 160 and 152 px from centre.
        Assert.All(
            model.Planets,
            planet =>
            {
                Assert.InRange(planet.Position.X, 256 - 160, 256 + 160);
                Assert.InRange(planet.Position.Y, 192f - 152, 192f + 152);
            });
    }

    [Fact]
    public void ResetNamesNoMorePlanetsThanTherAreRowsToNameThemIn()
    {
        ShortRangeChartController controller = CreateController(out _, out _);

        controller.Reset();

        // Names are packed one to a row, so two planets sharing a row means one of them goes unnamed.
        ShortRangeChartModel model = controller.BuildModel();
        Assert.True(model.Labels.Count <= model.Planets.Count);
        Assert.True(model.Labels.Count <= 64 - 4);
    }

    // Pins a fix: a planet's label row going negative used to throw rather than leaving it unnamed.
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void ResetLeavesPlanetsPlottedOutsideTheChartUnnamed(int designScale)
    {
        ShortRangeChartController controller = CreateController(
            out _,
            out _,
            new ViewLayout(512, 512, 128, designScale));

        controller.Reset();

        // The point is that it returns at all; what it returns still has to make sense.
        ShortRangeChartModel model = controller.BuildModel();
        Assert.True(model.Labels.Count <= model.Planets.Count);
        Assert.True(model.Labels.Count <= 64 - 4);
    }

    [Fact]
    public void ResetPutsTheCrossOnTheHyperspacePlanet()
    {
        ShortRangeChartController controller = CreateController(out _, out GameState gameState);

        controller.Reset();

        Assert.Equal(
            new Vector2(
                ((gameState.HyperspacePlanet.D - gameState.DockedPlanet.D) * 8) + 256,
                ((gameState.HyperspacePlanet.B - gameState.DockedPlanet.B) * 4) + 192f),
            controller.Cross);
    }

    [Theory]
    [InlineData(ConsoleKey.UpArrow, 0, -4)]
    [InlineData(ConsoleKey.S, 0, -4)]
    [InlineData(ConsoleKey.DownArrow, 0, 4)]
    [InlineData(ConsoleKey.X, 0, 4)]
    [InlineData(ConsoleKey.LeftArrow, -4, 0)]
    [InlineData(ConsoleKey.OemComma, -4, 0)]
    [InlineData(ConsoleKey.RightArrow, 4, 0)]
    [InlineData(ConsoleKey.OemPeriod, 4, 0)]
    public void EachDirectionKeyAndItsAliasStepTheCrossFourPixels(ConsoleKey key, float dx, float dy)
    {
        ShortRangeChartController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();
        CentreCross(controller, keyboard);
        Vector2 before = controller.Cross;

        keyboard.KeyDown(key, default);
        controller.HandleInput();

        Assert.Equal(before + new Vector2(dx, dy), controller.Cross);
    }

    [Theory]
    [InlineData(ConsoleKey.LeftArrow, MinX, false)]
    [InlineData(ConsoleKey.RightArrow, MaxX, false)]
    [InlineData(ConsoleKey.UpArrow, MinY, true)]
    [InlineData(ConsoleKey.DownArrow, MaxY, true)]
    public void TheCrossStopsAtTheEdgeOfTheChart(ConsoleKey key, float expected, bool isVertical)
    {
        ShortRangeChartController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();

        // Far more presses than the box is wide, so the clamp stops it, not the count.
        for (int i = 0; i < 300; i++)
        {
            keyboard.KeyDown(key, default);
            controller.HandleInput();
        }

        Assert.Equal(expected, isVertical ? controller.Cross.Y : controller.Cross.X);
    }

    [Fact]
    public void TheOriginKeyPutsTheCrossOnTheCentreOfTheChart()
    {
        ShortRangeChartController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.O, default);
        controller.HandleInput();

        // O recentres then measures; measuring snaps the cross onto whichever planet the centre named.
        Assert.InRange(controller.Cross.X, MinX, MaxX);
        Assert.InRange(controller.Cross.Y, MinY, MaxY);
        Assert.NotEmpty(controller.BuildModel().Caption);
    }

    [Fact]
    public void TheFindPromptCapturesTheTypedNameAndShowsItAsTheDetail()
    {
        ShortRangeChartController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.F, default);
        controller.HandleInput();

        Assert.Equal("Planet Name?", controller.BuildModel().Caption);

        Type(controller, keyboard, ConsoleKey.L, ConsoleKey.A);

        Assert.Equal("LA", controller.BuildModel().Detail);
    }

    [Fact]
    public void FindBackspaceRemovesTheLastCharacterAndStopsAtEmpty()
    {
        ShortRangeChartController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.F, default);
        controller.HandleInput();
        Type(controller, keyboard, ConsoleKey.L);

        Press(controller, keyboard, ConsoleKey.Backspace);
        Assert.Equal(string.Empty, controller.BuildModel().Detail);

        // An empty name has nothing to delete, and must not underflow.
        Press(controller, keyboard, ConsoleKey.Backspace);
        Assert.Equal(string.Empty, controller.BuildModel().Detail);
    }

    [Fact]
    public void FindingANameThatIsNotAPlanetLeavesTheChartWithNoPlanetNamed()
    {
        ShortRangeChartController controller = CreateController(out FakeKeyboard keyboard, out _);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.F, default);
        controller.HandleInput();
        Type(controller, keyboard, ConsoleKey.Q, ConsoleKey.Q, ConsoleKey.Q);
        Press(controller, keyboard, ConsoleKey.Enter);

        Assert.Equal("Unknown Planet", controller.BuildModel().Caption);
    }

    [Fact]
    public void FindingAPlanetOnTheChartMovesTheCrossOntoIt()
    {
        ShortRangeChartController controller = CreateController(out FakeKeyboard keyboard, out GameState gameState);
        controller.Reset();

        // The planet the chart opened on, so the search is bound to find it however the seeds fall.
        string target = gameState.PlanetName.ToUpperInvariant();
        Assert.NotEmpty(target);

        keyboard.KeyDown(ConsoleKey.F, default);
        controller.HandleInput();
        Type(controller, keyboard, [.. target.Select(c => Enum.Parse<ConsoleKey>(c.ToString()))]);
        Press(controller, keyboard, ConsoleKey.Enter);

        Assert.Equal(target, controller.BuildModel().Caption.ToUpperInvariant());
        Assert.Equal(
            new Vector2(
                ((gameState.HyperspacePlanet.D - gameState.DockedPlanet.D) * 8) + 256,
                ((gameState.HyperspacePlanet.B - gameState.DockedPlanet.B) * 4) + 192f),
            controller.Cross);
    }

    [Fact]
    public void MovingTheCrossDefersTheDistanceReadoutForFiveUpdates()
    {
        ShortRangeChartController controller = CreateController(out FakeKeyboard keyboard, out GameState gameState);
        controller.Reset();
        CentreCross(controller, keyboard);

        Press(controller, keyboard, ConsoleKey.RightArrow);
        gameState.PlanetName = "STALE";

        // The readout is left alone while the player moves; the name catches up on the fifth update after the last press.
        for (int i = 0; i < 4; i++)
        {
            controller.Update();
            Assert.Equal("STALE", controller.BuildModel().Caption);
        }

        controller.Update();
        Assert.NotEqual("STALE", controller.BuildModel().Caption);
    }

    [Fact]
    public void AnIdleChartDoesNotKeepRemeasuring()
    {
        ShortRangeChartController controller = CreateController(out _, out GameState gameState);
        controller.Reset();
        gameState.PlanetName = "STALE";

        // No key has been pressed, so there is no timer to run down.
        for (int i = 0; i < 10; i++)
        {
            controller.Update();
        }

        Assert.Equal("STALE", controller.BuildModel().Caption);
    }

    [Fact]
    public void TheDistanceIsPrintedInLightYearsAndOmittedWhenThereIsNone()
    {
        ShortRangeChartController controller = CreateController(out FakeKeyboard keyboard, out GameState gameState);
        controller.Reset();

        // At the start of a game the cross starts on the planet it's docked at: no distance to print.
        Assert.Equal(0, gameState.DistanceToPlanet);
        Assert.Equal(string.Empty, controller.BuildModel().Detail);

        // Walk it well clear of the planet it started on.
        for (int i = 0; i < 20; i++)
        {
            keyboard.KeyDown(ConsoleKey.RightArrow, default);
            controller.HandleInput();
        }

        Press(controller, keyboard, ConsoleKey.D);

        Assert.True(gameState.DistanceToPlanet > 0);
        Assert.Equal(
            $"Distance: {gameState.DistanceToPlanet:N1} Light Years",
            controller.BuildModel().Detail);
    }

    [Fact]
    public void TheEightBitTiersChartIsTheSixteenBitOneHalved()
    {
        // The controller derives its bounds from the tier's scale, the whole reason one controller serves both.
        ShortRangeChartController controller = CreateController(
            out FakeKeyboard keyboard,
            out _,
            new ViewLayout(320, 256, 56, 1));
        controller.Reset();

        for (int i = 0; i < 300; i++)
        {
            keyboard.KeyDown(ConsoleKey.UpArrow, default);
            controller.HandleInput();
        }

        // (18 * 1) + 1, against the 16-bit tier's (18 * 2) + 1.
        Assert.Equal(19, controller.Cross.Y);
    }

    [Fact]
    public void DrawHandsTheViewTheModelItWouldHaveBuilt()
    {
        ShortRangeChartController controller = CreateController(out _, out _, out FakeShortRangeChartView view);
        controller.Reset();

        controller.Draw();

        Assert.Equal(controller.BuildModel(), view.Drawn);
    }

    private static void Press(ShortRangeChartController controller, FakeKeyboard keyboard, ConsoleKey key)
    {
        keyboard.ClearPressed();
        keyboard.KeyDown(key, default);
        controller.HandleInput();
        keyboard.ClearPressed();
    }

    private static void Type(ShortRangeChartController controller, FakeKeyboard keyboard, params ConsoleKey[] keys)
    {
        foreach (ConsoleKey key in keys)
        {
            Press(controller, keyboard, key);
        }
    }

    // The starting planet can be against an edge; walk it to the middle so a clamp cannot hide a step.
    private static void CentreCross(ShortRangeChartController controller, FakeKeyboard keyboard)
    {
        keyboard.KeyDown(ConsoleKey.O, default);
        controller.HandleInput();
        keyboard.ClearPressed();
    }

    private static ShortRangeChartController CreateController(
        out FakeKeyboard keyboard,
        out GameState gameState,
        ViewLayout? layout = null)
        => CreateController(out keyboard, out gameState, out _, layout);

    private static ShortRangeChartController CreateController(
        out FakeKeyboard keyboard,
        out GameState gameState,
        out FakeShortRangeChartView view,
        ViewLayout? layout = null)
    {
        keyboard = new FakeKeyboard();
        ScreenManager<Screen, IScreenController> views = new(keyboard);
        gameState = new(views, TestMissions.Registry());
        FakeEliteDraw draw = new();
        if (layout is not null)
        {
            draw.Layout = layout;
        }

        // The classic first galaxy, so the chart has real planets: a bare GameState's all-zero seed
        // generates 256 nameless planets stacked at (0, 0).
        gameState.Cmdr.Galaxy = new GalaxySeed { A = 0x4A, B = 0x5A, C = 0x48, D = 0x02, E = 0x53, F = 0xB7 };
        PlanetController planet = new(gameState);
        view = new FakeShortRangeChartView();
        gameState.DockedPlanet = planet.FindPlanet(gameState.Cmdr.Galaxy, new Vector2(0x60, 0x60));
        gameState.HyperspacePlanet = new GalaxySeed(gameState.DockedPlanet);

        return new ShortRangeChartController(
            gameState,
            draw,
            keyboard,
            planet,
            new PlayerShip(gameState),
            view);
    }

    // Records rather than draws: what reaches the view is the only thing
    // Draw does.
    private sealed class FakeShortRangeChartView : IView<ShortRangeChartModel>
    {
        public ShortRangeChartModel? Drawn { get; private set; }

        public void Draw(ShortRangeChartModel model) => Drawn = model;
    }
}

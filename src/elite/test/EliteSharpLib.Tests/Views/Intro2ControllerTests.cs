// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Conflict;
using EliteSharpLib.Fakes;
using EliteSharpLib.Missions;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Fakes;
using SharpKind.Fakes.Audio;
using SharpKind.Fakes.Input;
using SharpKind.Input;

namespace EliteSharpLib.Tests.Views;

// The ship parade's behaviour, with no renderer involved: which ship's name
// is on show, and that it changes as the parade advances.
public class Intro2ControllerTests
{
    [Fact]
    public void NoShipNameBeforeTheFirstReset()
    {
        Intro2Controller controller = CreateController();

        Assert.Equal(string.Empty, controller.BuildModel().ShipName);
    }

    [Fact]
    public void ResetPutsTheFirstParadeShipInTheUniverse()
    {
        Intro2Controller controller = CreateController();

        controller.Reset();

        Assert.NotEqual(string.Empty, controller.BuildModel().ShipName);
    }

    [Fact]
    public void RightArrowKeepsAShipOnShow()
    {
        // FakeShipFactory.CreateParade returns a single ship, so this only pins that a ship stays in view.
        Intro2Controller controller = CreateController(out FakeKeyboard keyboard);
        controller.Reset();

        keyboard.KeyDown(ConsoleKey.RightArrow, default);
        controller.HandleInput();

        Assert.NotEqual(string.Empty, controller.BuildModel().ShipName);
    }

    [Fact]
    public void ThePromptIsFixedWording()
    {
        Intro2Controller controller = CreateController();

        Assert.Equal("Press Fire or Space, Commander.", controller.BuildModel().Prompt);
    }

    [Fact]
    public void TheFireButtonStartsTheGame()
    {
        // The prompt says "Press Fire or Space", so a joystick's fire button
        // has to leave the parade just as the spacebar does.
        Intro2Controller controller = CreateController(
            out _,
            out FakeGamepad gamepad,
            out ScreenManager<Screen, IScreenController> views);
        views.Add(Screen.CommanderStatus, new FakeScreenController());
        controller.Reset();

        gamepad.ButtonDown(GamepadButton.A);
        controller.HandleInput();

        Assert.Equal(Screen.CommanderStatus, views.CurrentId);
    }

    private static Intro2Controller CreateController() => CreateController(out _, out _, out _);

    private static Intro2Controller CreateController(out FakeKeyboard keyboard)
        => CreateController(out keyboard, out _, out _);

    private static Intro2Controller CreateController(
        out FakeKeyboard keyboard,
        out FakeGamepad gamepad,
        out ScreenManager<Screen, IScreenController> views)
    {
        keyboard = new FakeKeyboard();
        gamepad = new FakeGamepad();
        views = new(keyboard);
        GameState gameState = new(views, TestMissions.Registry());
        PlayerShip ship = new(gameState);
        FakeEliteDraw draw = new();
        RNG rng = new(new FakeRandomSource());
        FakeShipFactory shipFactory = new(draw);
        Universe universe = new(shipFactory, rng);
        AudioController audio = new(new FakeSound(), new Dictionary<string, SfxSample>(), new());
        Combat combat = CreateCombat(gameState, audio, ship, universe, draw, shipFactory, rng);
        Stars stars = new(gameState, draw, ship, new SixteenBitRendition().CreateStarfieldRenderer(draw));

        return new Intro2Controller(
            gameState, audio, keyboard, gamepad, stars, ship, combat, universe, shipFactory, new FakeIntro2View());
    }

    // Split out of CreateController only to keep its type coupling inside the
    // analyser's limit; nothing here is under test.
    private static Combat CreateCombat(
        GameState gameState,
        AudioController audio,
        PlayerShip ship,
        Universe universe,
        FakeEliteDraw draw,
        FakeShipFactory shipFactory,
        RNG rng)
    {
        Pilot pilot = new(draw, audio, universe, ship, gameState);
        Trade trade = TestGoods.Trade(gameState, ship);
        MissionRunner missions = TestMissions.Runner(gameState, ship, trade);

        return new Combat(
            gameState,
            audio,
            ship,
            trade,
            pilot,
            universe,
            draw,
            new SixteenBitRendition(),
            shipFactory,
            rng,
            missions);
    }

    // Somewhere for the parade to leave to; nothing about it is under test.
    private sealed class FakeScreenController : IScreenController
    {
        public void Draw()
        {
        }

        public void HandleInput()
        {
        }

        public void Reset()
        {
        }

        public void Update()
        {
        }
    }

    private sealed class FakeIntro2View : IView<Intro2Model>
    {
        public void Draw(Intro2Model model)
        {
            // Drawing is not under test here.
        }
    }
}

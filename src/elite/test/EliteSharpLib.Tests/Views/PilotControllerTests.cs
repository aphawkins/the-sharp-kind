// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Ships;
using EliteSharp.Abstractions.Views;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Conflict;
using EliteSharpLib.Controls;
using EliteSharpLib.Fakes;
using EliteSharpLib.Graphics;
using EliteSharpLib.Lasers;
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

// One controller serves all four cockpit windows; these tests exercise that
// each PilotDirection selects its own view name and laser mount, with no
// renderer involved.
public class PilotControllerTests
{
    private static readonly SixteenBitRendition s_rendition = new();

    [Fact]
    public void EachDirectionNamesItsOwnView()
    {
        Assert.Equal("Front View", CreateController(PilotDirection.Front, out _).BuildModel().ViewName);
        Assert.Equal("Rear View", CreateController(PilotDirection.Rear, out _).BuildModel().ViewName);
        Assert.Equal("Left View", CreateController(PilotDirection.Left, out _).BuildModel().ViewName);
        Assert.Equal("Right View", CreateController(PilotDirection.Right, out _).BuildModel().ViewName);
    }

    [Fact]
    public void EachDirectionDrawsItsOwnLaserMount()
    {
        PilotController front = CreateController(PilotDirection.Front, out PlayerShip frontShip);
        frontShip.LaserFront = new PulseLaser();
        PilotController rear = CreateController(PilotDirection.Rear, out PlayerShip rearShip);
        rearShip.LaserRear = new BeamLaser();

        Assert.Equal(LaserType.Pulse, front.BuildModel().LaserType);
        Assert.Equal(LaserType.Beam, rear.BuildModel().LaserType);
    }

    [Fact]
    public void NoHyperspaceStatusByDefault()
    {
        PilotController controller = CreateController(PilotDirection.Front, out _);

        Assert.Equal(string.Empty, controller.BuildModel().HyperspaceStatus);
    }

    [Fact]
    public void GalacticHyperspaceIsReportedOnceEngaged()
    {
        PilotController controller = CreateController(PilotDirection.Front, out PlayerShip ship, out Space space);
        ship.HasGalacticHyperdrive = true;

        space.StartGalacticHyperspace();

        Assert.Equal("Galactic Hyperspace", controller.BuildModel().HyperspaceStatus);
    }

    [Fact]
    public void FiringSetsTheLaserFramesWhichDecayOverThreeUpdates()
    {
        PilotController controller = CreateController(PilotDirection.Front, out _, out _, out GameState gameState);
        gameState.DrawLasers = true;

        controller.Update();
        Assert.True(controller.BuildModel().IsFiring);

        gameState.DrawLasers = false;
        controller.Update(); // frames: 2 -> 1
        Assert.True(controller.BuildModel().IsFiring);

        controller.Update(); // frames: 1 -> 0
        Assert.False(controller.BuildModel().IsFiring);
    }

    [Fact]
    public void TheLaserBoltStaysVisibleForTheSameLengthOfTime()
    {
        // The bolt is drawn for two of the game's ticks. Counted in updates
        // instead it was two updates, which at sixty frames a second is a
        // 33ms flicker where it should be about 150ms.
        PilotController controller = CreateController(
            PilotDirection.Front, out _, out _, out GameState state);

        state.Clock.BeginUpdate(1f / GameClock.StepsPerSecond / 4f);
        state.DrawLasers = true;
        controller.Update();
        state.DrawLasers = false;

        // Two ticks is eight quarter-tick updates; the first of them was the
        // one that lit it, so seven more keep it lit and the eighth does not.
        for (int update = 0; update < 7; update++)
        {
            controller.Update();
            Assert.True(controller.BuildModel().IsFiring, $"the bolt went out after {update + 1} updates");
        }

        controller.Update();

        Assert.False(controller.BuildModel().IsFiring);
    }

    // An analog stick is a position, not a key: the ship turns at the rate
    // the stick is pushed to, straight away, with none of the ramp a held
    // key climbs through. Roll is negated because the stick's X is positive
    // to the right and the roll rate is not.
    [Theory]
    [InlineData(1f, -31f)]
    [InlineData(-1f, 31f)]
    [InlineData(0.55f, -15.5f)]
    public void AnAnalogStickRollsAtTheRateItIsPushedTo(float x, float expected)
    {
        PilotController controller = CreateController(
            PilotDirection.Front, out PlayerShip ship, out _, out _, out FakeGamepad gamepad);
        MakeAnalog(gamepad, GamepadAxis.LeftX);
        gamepad.AxisMoved(GamepadAxis.LeftX, x);

        controller.HandleInput();

        Assert.Equal(expected, ship.Roll, 3);
    }

    // Pitch keeps the stick's own sense, so no negation - and it reaches the
    // rate in one update, where a key would need fifteen.
    [Fact]
    public void AnAnalogStickPitchesAtTheRateItIsPushedTo()
    {
        PilotController controller = CreateController(
            PilotDirection.Front, out PlayerShip ship, out _, out _, out FakeGamepad gamepad);
        MakeAnalog(gamepad, GamepadAxis.LeftY);
        gamepad.AxisMoved(GamepadAxis.LeftY, -1f);

        controller.HandleInput();

        Assert.Equal(-8f, ship.Pitch, 3);
    }

    // Released, the rate goes to nothing at once rather than bleeding away:
    // the stick is back at centre, so the commanded rate is zero.
    [Fact]
    public void AnAnalogStickReleasedStopsTheTurnAtOnce()
    {
        PilotController controller = CreateController(
            PilotDirection.Front, out PlayerShip ship, out _, out _, out FakeGamepad gamepad);
        MakeAnalog(gamepad, GamepadAxis.LeftX);
        gamepad.AxisMoved(GamepadAxis.LeftX, 1f);
        controller.HandleInput();

        gamepad.AxisMoved(GamepadAxis.LeftX, 0f);
        controller.HandleInput();
        ship.LevelOut();

        Assert.Equal(0f, ship.Roll, 3);
    }

    // The whole point of detecting the device: a digital stick still climbs
    // the ramp two units at a time, exactly as it did before analog existed.
    [Fact]
    public void ADigitalStickStillClimbsTheRamp()
    {
        PilotController controller = CreateController(
            PilotDirection.Front, out PlayerShip ship, out _, out _, out FakeGamepad gamepad);
        gamepad.AxisMoved(GamepadAxis.LeftX, -1f);

        controller.HandleInput();

        Assert.Equal(2f, ship.Roll, 3);
    }

    // A stick inside the deadzone is a stick at rest, so it neither turns the
    // ship nor claims the control from the keyboard.
    [Fact]
    public void AnAnalogStickWithinTheDeadzoneDoesNotRoll()
    {
        PilotController controller = CreateController(
            PilotDirection.Front, out PlayerShip ship, out _, out _, out FakeGamepad gamepad);
        MakeAnalog(gamepad, GamepadAxis.LeftX);
        gamepad.AxisMoved(GamepadAxis.LeftX, 0.03f);

        controller.HandleInput();

        Assert.Equal(0f, ship.Roll, 3);
    }

    // A stick left plugged in and centred commands a rate of zero, which
    // must not lock the keyboard out of the same control.
    [Fact]
    public void TheKeyboardStillRollsWithACentredAnalogStickPluggedIn()
    {
        PilotController controller = CreateController(
            PilotDirection.Front, out PlayerShip ship, out _, out _, out FakeGamepad gamepad, out FakeKeyboard keyboard);
        MakeAnalog(gamepad, GamepadAxis.LeftX);
        gamepad.AxisMoved(GamepadAxis.LeftX, 0f);
        keyboard.KeyDown(ConsoleKey.OemComma, default);

        controller.HandleInput();

        Assert.Equal(2f, ship.Roll, 3);
    }

    // A throttle lever is a position, so the speed is wherever the lever is
    // set. Forward is fast, and SDL reports forward as negative.
    [Theory]
    [InlineData(-1f, 40f)]
    [InlineData(0f, 20f)]
    [InlineData(1f, 0f)]
    public void TheThrottleLeverSetsTheSpeedOutright(float axis, float expected)
    {
        PilotController controller = CreateController(
            PilotDirection.Front, out PlayerShip ship, out _, out GameState gameState, out FakeGamepad gamepad);
        gameState.IsDocked = false;
        gamepad.Connected("Microsoft SideWinder Precision 2 Joystick");
        gamepad.AxisMoved(GamepadAxis.Throttle, 0.3f);
        gamepad.AxisMoved(GamepadAxis.Throttle, axis);

        controller.HandleInput();

        Assert.Equal(expected, ship.Speed, 3);
    }

    // Docked, the lever must not drive the ship. It holds its position, so
    // without this it would set a speed the moment the game started.
    [Fact]
    public void TheThrottleLeverIsIgnoredWhileDocked()
    {
        PilotController controller = CreateController(
            PilotDirection.Front, out PlayerShip ship, out _, out GameState gameState, out FakeGamepad gamepad);
        gameState.IsDocked = true;
        gamepad.Connected("Microsoft SideWinder Precision 2 Joystick");
        gamepad.AxisMoved(GamepadAxis.Throttle, 0.3f);
        gamepad.AxisMoved(GamepadAxis.Throttle, -1f);

        controller.HandleInput();

        Assert.Equal(0f, ship.Speed, 3);
    }

    // A device with no lever keeps the buttons, and must not be given a
    // half speed by an axis that reads zero because it does not exist.
    [Fact]
    public void ADeviceWithNoLeverDoesNotTouchTheSpeed()
    {
        PilotController controller = CreateController(
            PilotDirection.Front, out PlayerShip ship, out _, out GameState gameState, out FakeGamepad gamepad);
        gameState.IsDocked = false;
        gamepad.Connected("Xbox One Controller");

        controller.HandleInput();

        Assert.Equal(0f, ship.Speed, 3);
    }

    // The stick spends one button on what the keyboard spends two keys on,
    // so the same button has to mean both halves in turn.
    [Fact]
    public void TheDockingButtonEngagesThenDisengages()
    {
        PilotController controller = CreateController(
            PilotDirection.Front,
            out PlayerShip ship,
            out _,
            out GameState gameState,
            out FakeGamepad gamepad,
            out _,
            out Pilot pilot);
        gameState.IsDocked = false;
        gameState.Config.Game.InstantDock = false;
        ship.HasDockingComputer = true;
        gamepad.Connected("Microsoft SideWinder Precision 2 Joystick");

        gamepad.ButtonDown(GamepadButton.Back);
        controller.HandleInput();
        Assert.True(pilot.IsAutoPilotOn);

        // Released and pressed again: a held button must not toggle back on
        // the very next update.
        controller.HandleInput();
        Assert.True(pilot.IsAutoPilotOn);

        gamepad.ButtonUp(GamepadButton.Back);
        controller.HandleInput();
        gamepad.ButtonDown(GamepadButton.Back);
        controller.HandleInput();

        Assert.False(pilot.IsAutoPilotOn);
    }

    // Without a docking computer fitted the button does nothing, the same
    // as the C key.
    [Fact]
    public void TheDockingButtonNeedsTheComputerFitted()
    {
        PilotController controller = CreateController(
            PilotDirection.Front,
            out PlayerShip ship,
            out _,
            out GameState gameState,
            out FakeGamepad gamepad,
            out _,
            out Pilot pilot);
        gameState.IsDocked = false;
        ship.HasDockingComputer = false;
        gamepad.Connected("Microsoft SideWinder Precision 2 Joystick");

        gamepad.ButtonDown(GamepadButton.Back);
        controller.HandleInput();

        Assert.False(pilot.IsAutoPilotOn);
    }

    private static PilotController CreateController(PilotDirection direction, out PlayerShip ship)
        => CreateController(direction, out ship, out _);

    private static PilotController CreateController(PilotDirection direction, out PlayerShip ship, out Space space)
        => CreateController(direction, out ship, out space, out _);

    private static PilotController CreateController(
        PilotDirection direction, out PlayerShip ship, out Space space, out GameState gameState)
        => CreateController(direction, out ship, out space, out gameState, out _);

    private static PilotController CreateController(
        PilotDirection direction,
        out PlayerShip ship,
        out Space space,
        out GameState gameState,
        out FakeGamepad gamepad)
        => CreateController(direction, out ship, out space, out gameState, out gamepad, out _);

    private static PilotController CreateController(
        PilotDirection direction,
        out PlayerShip ship,
        out Space space,
        out GameState gameState,
        out FakeGamepad gamepad,
        out FakeKeyboard keyboard)
        => CreateController(direction, out ship, out space, out gameState, out gamepad, out keyboard, out _);

    private static PilotController CreateController(
        PilotDirection direction,
        out PlayerShip ship,
        out Space space,
        out GameState gameState,
        out FakeGamepad gamepad,
        out FakeKeyboard keyboard,
        out Pilot pilotOut)
    {
        gamepad = new FakeGamepad();
        keyboard = new FakeKeyboard();
        ScreenManager<Screen, IScreenController> views = new(new FakeKeyboard());
        gameState = new(views, TestMissions.Registry());
        ship = new PlayerShip(gameState);
        Trade trade = TestGoods.Trade(gameState, ship);
        FakeEliteDraw draw = new();
        RNG rng = new(new FakeRandomSource());
        FakeShipFactory shipFactory = new(draw);
        Universe universe = new(shipFactory, rng);
        AudioController audio = new(new FakeSound(), new Dictionary<string, SfxSample>(), new());
        Stars stars = CreateStars(gameState, draw, ship);
        Pilot pilot = new(draw, audio, universe, ship, gameState);
        MissionRunner missions = TestMissions.Runner(gameState, ship, trade);

        Combat combat = new(
            gameState,
            audio,
            ship,
            trade,
            pilot,
            universe,
            draw,
            s_rendition,
            shipFactory,
            rng,
            missions);
        space = new(
            gameState,
            audio,
            pilot,
            combat,
            trade,
            ship,
            new PlanetController(gameState),
            stars,
            universe,
            draw,
            s_rendition,
            rng);

        pilotOut = pilot;

        return NewController(
            gameState, pilot, ship, stars, space, combat, direction, draw, ShippedControls(keyboard, gamepad), keyboard);
    }

    // Kept out of CreateController for the same reason CreateStars is: the
    // controller and its fakes are several more types than that method's
    // coupling budget has room for.
    private static PilotController NewController(
        GameState gameState,
        Pilot pilot,
        PlayerShip ship,
        Stars stars,
        Space space,
        Combat combat,
        PilotDirection direction,
        IEliteDraw draw,
        EliteControlMap controls,
        FakeKeyboard keyboard)
        => new(
            gameState,
            controls,
            keyboard,
            pilot,
            ship,
            stars,
            space,
            combat,
            direction,
            draw,
            new FakePilotView());

    // Kept out of CreateController for the same reason CreateStars is: the
    // map and its bindings are two more types than that method's coupling
    // budget has room for. The shipped bindings, so the tests exercise what
    // a commander actually gets.
    private static EliteControlMap ShippedControls(FakeKeyboard keyboard, FakeGamepad gamepad)
        => new(EliteControlDefaults.Create(), keyboard, gamepad);

    // Kept out of CreateController, which is already at CA1506's coupling
    // limit: the starfield renderer adds two more types to whichever method
    // names it.
    private static Stars CreateStars(GameState gameState, FakeEliteDraw draw, PlayerShip ship)
        => new(gameState, draw, ship, s_rendition.CreateStarfieldRenderer(draw));

    // An axis only counts as analog once it has reported a position between
    // its extremes; this is that proof, and it is what the tests above need
    // before the stick's position means anything.
    private static void MakeAnalog(FakeGamepad gamepad, GamepadAxis axis)
        => gamepad.AxisMoved(axis, 0.3f);

    private sealed class FakePilotView : IView<PilotModel>
    {
        public void Draw(PilotModel model)
        {
            // Drawing is not under test here.
        }
    }
}

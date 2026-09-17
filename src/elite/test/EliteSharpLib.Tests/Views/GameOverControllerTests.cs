// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Audio;
using EliteSharpLib.Conflict;
using EliteSharpLib.Fakes;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Fakes;
using SharpKind.Fakes.Audio;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests.Views;

// A dead Cobra and five pieces of cargo tumble for a hundred ticks, then the game resets.
// The wait is counted in ticks rather than updates, so a faster frame rate must not shorten it.
public class GameOverControllerTests
{
    [Fact]
    public void ResetPutsADeadCobraAndFivePiecesOfCargoInFrontOfThePlayer()
    {
        GameOverController controller = CreateController(out _, out Universe universe, out _);

        controller.Reset();

        List<IObject> objects = [.. universe.GetAllObjects()];

        // The wreck and five pieces of cargo, all out at the same distance,
        // and only the wreck already dead.
        Assert.Equal(6, objects.Count);
        IObject cobra = Assert.Single(objects, o => o.Flags.HasFlag(ShipProperties.Dead));
        Assert.Equal(0, cobra.Location.X);
        Assert.Equal(0, cobra.Location.Y);
        Assert.All(objects, o => Assert.Equal(-400, o.Location.Z));
    }

    [Fact]
    public void ResetScattersTheCargoAndSetsItTumbling()
    {
        // Pins that offsets and spins are applied at all, not their distribution.
        GameOverController controller = CreateController(out _, out Universe universe, out _, randomValue: 32);

        controller.Reset();

        Assert.All(
            universe.GetAllObjects().Where(o => !o.Flags.HasFlag(ShipProperties.Dead)),
            cargo =>
            {
                Assert.NotEqual(0, cargo.Location.X);
                Assert.NotEqual(0, cargo.Location.Y);
                Assert.NotEqual(0, cargo.RotX);
                Assert.NotEqual(0, cargo.RotZ);
            });
    }

    [Fact]
    public void ResetStopsThePlayerRollingAndPitchingAndLeavesTheShipDrifting()
    {
        GameOverController controller = CreateController(out _, out _, out PlayerShip ship);
        ship.Roll = 5;
        ship.Pitch = 5;
        ship.Speed = 30;

        controller.Reset();

        Assert.Equal(0, ship.Roll);
        Assert.Equal(0, ship.Pitch);
        Assert.Equal(6, ship.Speed);
    }

    [Fact]
    public void ResetPlaysTheGameOverEffect()
    {
        GameOverController controller = CreateController(out _, out _, out _, out FakeSound sound);

        controller.Reset();

        Assert.Equal(1, sound.PlayCount(nameof(SoundEffect.Gameover)));
    }

    [Fact]
    public void TheGameIsStillRunningOnTheLastTickOfTheWreckage()
    {
        GameOverController controller = CreateController(out GameState gameState, out _, out _);
        controller.Reset();
        gameState.IsInitialised = true;

        Tick(controller, gameState, 100);

        Assert.True(gameState.IsInitialised);
    }

    [Fact]
    public void TheGameResetsOnceTheWreckageHasTumbledForAHundredTicks()
    {
        GameOverController controller = CreateController(out GameState gameState, out _, out _);
        controller.Reset();
        gameState.IsInitialised = true;

        Tick(controller, gameState, 101);

        Assert.False(gameState.IsInitialised);
    }

    [Fact]
    public void TheWaitIsTheSameLengthOfTimeAtAFasterFrameRate()
    {
        // A hundred ticks, not a hundred updates: at four times the rate it takes four times as many updates.
        GameOverController controller = CreateController(out GameState gameState, out _, out _);
        controller.Reset();
        gameState.IsInitialised = true;

        Tick(controller, gameState, 101, GameClock.StepsPerSecond * 4);
        Assert.True(gameState.IsInitialised);

        Tick(controller, gameState, 303, GameClock.StepsPerSecond * 4);
        Assert.False(gameState.IsInitialised);
    }

    [Fact]
    public void TheScreenSaysGameOverAndNothingElse()
    {
        GameOverController controller = CreateController(out _, out _, out _, out _, out FakeGameOverView view);

        controller.Draw();

        Assert.Equal("GAME OVER", view.Drawn?.Message);
    }

    // Nothing on this screen answers to the keyboard: HandleInput must leave the wreckage alone.
    [Fact]
    public void NoKeyDoesAnything()
    {
        GameOverController controller = CreateController(out GameState gameState, out Universe universe, out _);
        controller.Reset();
        gameState.IsInitialised = true;

        controller.HandleInput();

        Assert.Equal(6, universe.GetAllObjects().Count());
        Assert.True(gameState.IsInitialised);
    }

    // Opens each update at the given rate, the way EliteMain's loop does, so
    // the clock hands the screen the fraction of a tick the update is worth.
    private static void Tick(
        GameOverController controller,
        GameState gameState,
        int updates,
        float updatesPerSecond = GameClock.StepsPerSecond)
    {
        for (int i = 0; i < updates; i++)
        {
            gameState.Clock.BeginUpdate(1f / updatesPerSecond);
            controller.Update();
        }
    }

    private static GameOverController CreateController(
        out GameState gameState,
        out Universe universe,
        out PlayerShip ship,
        int randomValue = 1)
        => Build(out gameState, out universe, out ship, out _, out _, randomValue);

    private static GameOverController CreateController(
        out GameState gameState,
        out Universe universe,
        out PlayerShip ship,
        out FakeSound sound)
        => Build(out gameState, out universe, out ship, out sound, out _, 1);

    private static GameOverController CreateController(
        out GameState gameState,
        out Universe universe,
        out PlayerShip ship,
        out FakeSound sound,
        out FakeGameOverView view)
        => Build(out gameState, out universe, out ship, out sound, out view, 1);

    private static GameOverController Build(
        out GameState gameState,
        out Universe universe,
        out PlayerShip ship,
        out FakeSound sound,
        out FakeGameOverView view,
        int randomValue)
    {
        ScreenManager<Screen, IScreenController> views = new(new FakeKeyboard());
        gameState = new(views, TestMissions.Registry());
        ship = new PlayerShip(gameState);
        Trade trade = TestGoods.Trade(gameState, ship);
        FakeEliteDraw draw = new();
        RNG rng = new(new FakeRandomSource { RandomValue = randomValue });
        FakeShipFactory shipFactory = new(draw);
        universe = new Universe(shipFactory, rng);
        sound = new FakeSound();

        // AudioController looks a sample up whether or not effects are
        // switched on, so the one this screen plays has to be there.
        Dictionary<string, SfxSample> sfx = new()
        {
            { nameof(SoundEffect.Gameover), new(17) },
        };
        AudioController audio = new(sound, sfx, new());
        Pilot pilot = new(draw, audio, universe, ship, gameState);
        Stars stars = new(gameState, draw, ship, new SixteenBitRendition().CreateStarfieldRenderer(draw));
        Combat combat = new(
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
            TestMissions.Runner(gameState, ship, trade));

        view = new FakeGameOverView();

        return new GameOverController(
            gameState,
            audio,
            stars,
            ship,
            combat,
            universe,
            shipFactory,
            rng,
            view);
    }

    // Records rather than draws: the wording is the whole model.
    private sealed class FakeGameOverView : IView<GameOverModel>
    {
        public GameOverModel? Drawn { get; private set; }

        public void Draw(GameOverModel model) => Drawn = model;
    }
}

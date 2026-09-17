// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Audio;
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

// The alert counter belongs to the controller, so both tiers stop showing it on the same tick.
public class EscapeCapsuleControllerTests
{
    [Fact]
    public void TheAlertIsUpAsSoonAsTheCapsuleLaunches()
    {
        EscapeCapsuleController controller = CreateController();

        controller.Reset();

        Assert.True(controller.BuildModel().IsAlertVisible);
    }

    [Fact]
    public void TheAlertIsStillUpOnTheLastTickOfTheLaunch()
    {
        EscapeCapsuleController controller = CreateController();
        controller.Reset();

        Tick(controller, 89);

        Assert.True(controller.BuildModel().IsAlertVisible);
    }

    [Fact]
    public void TheAlertLapsesAfterNinetyTicks()
    {
        EscapeCapsuleController controller = CreateController();
        controller.Reset();

        Tick(controller, 90);

        Assert.False(controller.BuildModel().IsAlertVisible);
    }

    [Fact]
    public void TheAbandonedShipStillBlowsUpWhenTheRateIsNotAWholeTick()
    {
        // At a third of a tick the count steps from just under forty to just over, never landing on it.
        EscapeCapsuleController controller = CreateController(out GameState state, out Universe universe);
        controller.Reset();

        state.Clock.BeginUpdate(1f / GameClock.StepsPerSecond / 3f);
        Tick(controller, 3 * 45);

        Assert.Contains(
            universe.GetAllObjects(),
            obj => obj.Id == "CobraMk3" && obj.Flags.HasFlag(ShipProperties.Dead));
    }

    [Fact]
    public void TheAlertLapsesAfterTheSameTimeWhateverTheRate()
    {
        EscapeCapsuleController controller = CreateController(out GameState state, out _);
        controller.Reset();

        // Half a tick at a time: ninety ticks take a hundred and eighty updates.
        state.Clock.BeginUpdate(1f / GameClock.StepsPerSecond / 2f);
        Tick(controller, 2 * 89);
        Assert.True(controller.BuildModel().IsAlertVisible);

        Tick(controller, 2);
        Assert.False(controller.BuildModel().IsAlertVisible);
    }

    private static void Tick(EscapeCapsuleController controller, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            controller.Update();
        }
    }

    // The sequence times itself in ticks; a test at a different rate needs the clock those ticks come from.
    private static EscapeCapsuleController CreateController(out GameState gameState, out Universe universe)
        => Build(out gameState, out universe);

    private static EscapeCapsuleController CreateController() => Build(out _, out _);

    private static EscapeCapsuleController Build(out GameState gameState, out Universe outUniverse)
    {
        ScreenManager<Screen, IScreenController> views = new(new FakeKeyboard());
        gameState = new(views, TestMissions.Registry());
        PlayerShip ship = new(gameState);
        Trade trade = TestGoods.Trade(gameState, ship);
        FakeEliteDraw draw = new();
        RNG rng = new(new FakeRandomSource());
        FakeShipFactory shipFactory = new(draw);
        Universe universe = new(shipFactory, rng);
        outUniverse = universe;

        // The two effects the sequence plays; AudioController looks a sample up
        // whether or not effects are switched on.
        Dictionary<string, SfxSample> sfx = new()
        {
            { nameof(SoundEffect.Launch), new(32) },
            { nameof(SoundEffect.Explode), new(23) },
        };
        AudioController audio = new(new FakeSound(), sfx, new());
        Stars stars = new(gameState, draw, ship, new SixteenBitRendition().CreateStarfieldRenderer(draw));
        Pilot pilot = new(draw, audio, universe, ship, gameState);

        return new EscapeCapsuleController(
            gameState,
            audio,
            stars,
            ship,
            trade,
            universe,
            pilot,
            draw,
            shipFactory,
            new FakeEscapeCapsuleView());
    }

    private sealed class FakeEscapeCapsuleView : IView<EscapeCapsuleModel>
    {
        public void Draw(EscapeCapsuleModel model)
        {
            // Drawing is not under test here.
        }
    }
}

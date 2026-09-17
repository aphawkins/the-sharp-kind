// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Config;
using EliteSharpLib.Conflict;
using EliteSharpLib.Fakes;
using EliteSharpLib.Missions;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Config;
using SharpKind.Fakes;
using SharpKind.Fakes.Audio;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests.Views;

// Shared setup for the two settings screens: both need a real Space (they
// rebuild the planet and sun in place) over a fake draw surface.
internal static class SettingsControllerFixture
{
    internal static Space CreateSpace(
        out GameState gameState,
        out FakeKeyboard keyboard,
        out FakeEliteDraw draw,
        out AudioController audio)
    {
        keyboard = new FakeKeyboard();
        ScreenManager<Screen, IScreenController> views = new(keyboard);
        views.Add(Screen.Docking, new FakeView());
        views.Add(Screen.GameOver, new FakeView());
        views.Add(Screen.Hyperspace, new FakeView());
        views.Add(Screen.Options, new FakeView());
        gameState = new(views, TestMissions.Registry());

        draw = new FakeEliteDraw();
        RNG rng = new(new FakeRandomSource());
        PlayerShip ship = new(gameState);
        Trade trade = TestGoods.Trade(gameState, ship);
        FakeShipFactory shipFactory = new(draw);
        Universe universe = new(shipFactory, rng);
        audio = new(new FakeSound(), new Dictionary<string, SfxSample>(), new());
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
            new SixteenBitRendition(),
            shipFactory,
            rng,
            missions);

        return new Space(
            gameState,
            audio,
            pilot,
            combat,
            trade,
            ship,
            new PlanetController(gameState),
            new Stars(gameState, draw, ship, new SixteenBitRendition().CreateStarfieldRenderer(draw)),
            universe,
            draw,
            new SixteenBitRendition(),
            rng);
    }

    // Settings screens have no view of their own: they build controls over the surface in the rendition's style.
    internal static IBaseView CreateBaseView(FakeEliteDraw draw)
        => new SixteenBitRendition().CreateBaseView(draw);

    internal static SettingsListStyle CreateStyle(FakeEliteDraw draw)
        => new SixteenBitRendition().CreateSettingsListStyle(draw);

    internal static ConfigFile<EliteConfig> CreateConfigFile(string configFileName)
        => new(
            Path.Combine(Path.GetTempPath(), "EliteSettingsViewTests_" + Guid.NewGuid().ToString("N")),
            configFileName);
}

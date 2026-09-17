// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharpLib.Conflict;
using EliteSharpLib.Graphics;
using EliteSharpLib.Missions;
using EliteSharpLib.Renditions;
using EliteSharpLib.Ships;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpKind.Audio;
using SharpKind.Input;

namespace EliteSharpLib;

// Sequence and flight screens, split out of EliteSplitScreensServiceCollectionExtensions once it
// tripped CA1506's class-coupling limit. Views have since gone to plugins, but the split stays.
internal static class EliteSplitAnimatedScreensServiceCollectionExtensions
{
    internal static void AddSplitAnimatedScreens(this IServiceCollection services)
    {
        services.AddSplitSequenceScreens();
        services.AddSplitFlightScreens();
    }

    // The screens that play out on their own: mission messages, and the
    // animated sequences that run to a tick count.
    private static void AddSplitSequenceScreens(this IServiceCollection services)
    {
        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().MissionBriefingView);
        services.AddSingleton(sp => new MissionBriefingController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<MissionRunner>(),
            sp.GetRequiredService<Combat>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<IShipFactory>(),
            sp.GetRequiredService<IMissionBriefingView>(),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<MissionBriefingController>()));

        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<EscapeCapsuleModel>());
        services.AddSingleton(sp => new EscapeCapsuleController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<AudioController>(),
            sp.GetRequiredService<Stars>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<Trade>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<Pilot>(),
            sp.GetRequiredService<IEliteDraw>(),
            sp.GetRequiredService<IShipFactory>(),
            sp.GetRequiredService<IView<EscapeCapsuleModel>>(),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<EscapeCapsuleController>()));

        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<GameOverModel>());
        services.AddSingleton(sp => new GameOverController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<AudioController>(),
            sp.GetRequiredService<Stars>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<Combat>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<IShipFactory>(),
            sp.GetRequiredService<RNG>(),
            sp.GetRequiredService<IView<GameOverModel>>(),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<GameOverController>()));
    }

    // Screens whose Update drives the universe directly rather than a screen-local timer. The
    // four cockpit windows share one IView<PilotModel>; PopulateScreens builds the four
    // PilotControllers directly since they aren't otherwise-distinct types.
    private static void AddSplitFlightScreens(this IServiceCollection services)
    {
        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<Intro2Model>());
        services.AddSingleton(sp => new Intro2Controller(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<AudioController>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<IGamepad>(),
            sp.GetRequiredService<Stars>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<Combat>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<IShipFactory>(),
            sp.GetRequiredService<IView<Intro2Model>>(),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<Intro2Controller>()));

        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<PilotModel>());
    }
}

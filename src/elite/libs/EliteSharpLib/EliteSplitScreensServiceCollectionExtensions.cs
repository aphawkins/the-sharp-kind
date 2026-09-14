// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using EliteSharpLib.Config;
using EliteSharpLib.Conflict;
using EliteSharpLib.Graphics;
using EliteSharpLib.Missions;
using EliteSharpLib.Renditions;
using EliteSharpLib.Save;
using EliteSharpLib.Ships;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpKind.Audio;
using SharpKind.Config;
using SharpKind.Input;

namespace EliteSharpLib;

// The screen registrations: each screen's view comes off the tier's rendition,
// and the controller is what goes into the screen map. Kept in its own class,
// separate from EliteServiceCollectionExtensions, because the combined
// registrations tripped CA1506's class-coupling limit - the same reason they
// are split across the methods below, and the reason the sequence and flight
// screens live in EliteSplitAnimatedScreensServiceCollectionExtensions rather
// than here.
//
// There is no tier branch left here. Which views these are was decided when
// the rendition was loaded, so this file no longer knows there is more than one
// tier - which is what adding a third tier should cost: an assembly, and
// nothing here.
internal static class EliteSplitScreensServiceCollectionExtensions
{
    internal static void AddSplitScreens(this IServiceCollection services)
    {
        services.AddSplitConsoleScreens();
        services.AddSplitStatusScreens();
        services.AddSplitMenuScreens();
        services.AddSplitTextEntryScreens();
        services.AddSplitAnimatedScreens();
    }

    // The screens the commander drives: charts, status, and the menus.
    private static void AddSplitConsoleScreens(this IServiceCollection services)
    {
        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<GalacticChartModel>());
        services.AddSingleton(sp => new GalacticChartController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<PlanetController>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<IView<GalacticChartModel>>()));

        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<ShortRangeChartModel>());
        services.AddSingleton(sp => new ShortRangeChartController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IEliteDraw>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<PlanetController>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<IView<ShortRangeChartModel>>()));

        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<QuitModel>());
        services.AddSingleton(sp => new QuitController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<IView<QuitModel>>()));

        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<Intro1Model>());
        services.AddSingleton(sp => new Intro1Controller(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<AudioController>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<Combat>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<IShipFactory>(),
            sp.GetRequiredService<IView<Intro1Model>>(),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<Intro1Controller>()));
    }

    // The screens that just report status: commander status, inventory and
    // planet data. Split from AddSplitConsoleScreens once their 8-bit views
    // pushed it over CA1506's per-method limit.
    private static void AddSplitStatusScreens(this IServiceCollection services)
    {
        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<CommanderStatusModel>());
        services.AddSingleton(sp => new CommanderStatusController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<Trade>(),
            sp.GetRequiredService<PlanetController>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<IView<CommanderStatusModel>>()));

        // The inventory has no view of its own either: the rendition supplies a
        // style and the game builds the list, as it does for the market.
        services.AddSingleton(sp => new InventoryController(
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<Trade>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<IBaseView>(),
            sp.GetRequiredService<IEliteDraw>(),
            sp.GetRequiredService<RenditionRegistry>().InventoryListStyle));

        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<PlanetDataModel>());
        services.AddSingleton(sp => new PlanetDataController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<PlanetController>(),
            sp.GetRequiredService<RNG>(),
            sp.GetRequiredService<MissionRunner>(),
            sp.GetRequiredService<IView<PlanetDataModel>>()));
    }

    // The menu screens: options, the market, equip-ship and the two settings
    // lists. All share the selection-cursor shape, and the settings pair
    // additionally share one SettingsListStyle registration.
    private static void AddSplitMenuScreens(this IServiceCollection services)
    {
        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<OptionsModel>());
        services.AddSingleton(sp => new OptionsController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<IView<OptionsModel>>()));

        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<CreditsModel>());
        services.AddSingleton(sp => new CreditsController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<IView<CreditsModel>>()));

        // The market has no view of its own: the rendition supplies a style and
        // the game builds the list, so this takes the surface and the style
        // where the other screens take a view.
        services.AddSingleton(sp => new MarketController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<Trade>(),
            sp.GetRequiredService<PlanetController>(),
            sp.GetRequiredService<IBaseView>(),
            sp.GetRequiredService<IEliteDraw>(),
            sp.GetRequiredService<RenditionRegistry>().MarketListStyle));

        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<EquipmentModel>());
        services.AddSingleton(sp => new EquipmentController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<Trade>(),
            sp.GetRequiredService<ScannerController>(),
            sp.GetRequiredService<IView<EquipmentModel>>()));

        // The settings screens have no view: the game owns their controls and
        // the rendition contributes only the style they are drawn in.
        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().SettingsListStyle);
        services.AddSingleton(sp => new SettingsController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<Space>(),
            sp.GetRequiredService<ConfigFile<EliteConfig>>(),
            sp.GetRequiredService<RenditionRegistry>().BaseView,
            sp.GetRequiredService<IEliteDraw>(),
            sp.GetRequiredService<SettingsListStyle>()));
        services.AddSingleton(sp => new EngineSettingsController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<Space>(),
            sp.GetRequiredService<AudioController>(),
            sp.GetRequiredService<ConfigFile<EliteConfig>>(),
            sp.GetRequiredService<InstalledRenditions>(),
            sp.GetRequiredService<IGamepad>(),
            sp.GetRequiredService<RenditionRegistry>().BaseView,
            sp.GetRequiredService<IEliteDraw>(),
            sp.GetRequiredService<SettingsListStyle>()));
    }

    // The name-typing screens: load and save commander.
    private static void AddSplitTextEntryScreens(this IServiceCollection services)
    {
        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<LoadCommanderModel>());
        services.AddSingleton(sp => new LoadCommanderController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<SaveFile>(),
            sp.GetRequiredService<IView<LoadCommanderModel>>()));

        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<SaveCommanderModel>());
        services.AddSingleton(sp => new SaveCommanderController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IKeyboard>(),
            sp.GetRequiredService<SaveFile>(),
            sp.GetRequiredService<IView<SaveCommanderModel>>()));
    }
}

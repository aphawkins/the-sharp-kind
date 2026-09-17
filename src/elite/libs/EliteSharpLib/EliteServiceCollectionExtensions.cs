// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Renditions;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Audio;
using EliteSharpLib.Config;
using EliteSharpLib.Conflict;
using EliteSharpLib.Controls;
using EliteSharpLib.Graphics;
using EliteSharpLib.Missions;
using EliteSharpLib.Renditions;
using EliteSharpLib.Save;
using EliteSharpLib.Ships;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpKind.Abstraction;
using SharpKind.Abstraction.Config;
using SharpKind.Audio;
using SharpKind.Config;
using SharpKind.Input;

namespace EliteSharpLib;

public static class EliteServiceCollectionExtensions
{
    private const string ConfigFileName = "elite.sharp";

    // EliteConfig is internal, so Program.Main can't construct a ConfigFile<EliteConfig> directly.
    public static IServiceCollection AddEliteConfig(this IServiceCollection services, string userDataPath)
        => services.AddSingleton(sp => new ConfigFile<EliteConfig>(
            userDataPath,
            ConfigFileName,
            RepairConfig,
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConfigFile<EliteConfig>>()));

    // Lets Program.Main (which needs SharpKind.SDL, a dependency EliteSharpLib deliberately lacks)
    // read backend/tier/window scale before the DI container exists.
    public static EngineConfigSettings ReadEngineSettings(string userDataPath, ILoggerFactory loggerFactory)
        => EngineConfigReader.Read<EliteConfig>(userDataPath, ConfigFileName, RepairConfig, loggerFactory);

    // The whole domain graph below is internal to EliteSharpLib, so it can only be registered from here.
    public static IServiceCollection AddEliteMain(this IServiceCollection services, InstalledRenditions renditions)
    {
        ArgumentNullException.ThrowIfNull(renditions);

        // Loaded before the container exists: the window is made at the size the rendition draws at.
        services.AddSingleton(renditions);
        services.AddSingleton(renditions.Chosen);
        services.AddEliteCore();
        services.AddEliteRendering();
        services.AddEliteSimulation();
        services.AddEliteViews();

        // Needs every view registered above; EliteMain no longer news up (or sees) any view.
        services.AddSingleton(sp =>
        {
            PopulateScreens(sp);

            return new EliteMain(
                sp.GetRequiredService<IAbstraction>(),
                sp.GetRequiredService<EliteControlMap>(),
                sp.GetRequiredService<GameState>(),
                sp.GetRequiredService<PlayerShip>(),
                sp.GetRequiredService<IEliteDraw>(),
                sp.GetRequiredService<IBaseView>(),
                sp.GetRequiredService<Universe>(),
                sp.GetRequiredService<Stars>(),
                sp.GetRequiredService<Pilot>(),
                sp.GetRequiredService<Combat>(),
                sp.GetRequiredService<SaveFile>(),
                sp.GetRequiredService<Space>(),
                sp.GetRequiredService<ScannerController>(),
                sp.GetRequiredService<AudioController>(),
                sp.GetRequiredService<PlanetController>());
        });
        services.AddSingleton<IGame>(sp => sp.GetRequiredService<EliteMain>());
        services.AddSingleton<IGameApp>(sp => sp.GetRequiredService<EliteMain>());
        return services;
    }

    // The app needs the rendition before the container exists, since the window is made at its size.
    public static InstalledRenditions LoadRendition(string name, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return RenditionLoader.LoadFrom(
            AppContext.BaseDirectory,
            name,
            loggerFactory.CreateLogger(typeof(RenditionLoader)));
    }

    // Both the engine and the game halves repair themselves; this is the
    // hook ConfigFile calls to do it.
    internal static bool RepairConfig(EliteConfig config) => config.Repair();

    private static void AddEliteCore(this IServiceCollection services)
    {
        // The single shared source of entropy: unseeded in production, replaceable via RNG's constructor seam in tests.
        services.AddSingleton(_ => Random.Shared);
        services.AddSingleton(sp => new RNG(sp.GetRequiredService<Random>()));

        services.AddSingleton(sp => new ScreenManager<Screen, IScreenController>(sp.GetRequiredService<IKeyboard>()));
        services.AddSingleton(sp =>
        {
            EliteConfig config = sp.GetRequiredService<ConfigFile<EliteConfig>>().ReadConfig();

            // The rendition owns the scales on offer, so the settings screen shows the scale actually used.
            config.Engine.WindowScale = WindowScales.Resolve(
                sp.GetRequiredService<IRendition>(),
                config.Engine.WindowScale);

            return new GameState(
                sp.GetRequiredService<ScreenManager<Screen, IScreenController>>(),
                sp.GetRequiredService<MissionRegistry>())
            {
                Config = config,
            };
        });
        services.AddSingleton(sp => new PlayerShip(sp.GetRequiredService<GameState>()));

        // A plugin like missions/renditions, but unlike a mission a missing goods set is fatal - no market without one.
        services.AddSingleton(sp => new GoodsRegistry(
            GoodsLoader.LoadFrom(
                AppContext.BaseDirectory,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(GoodsLoader))),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<GoodsRegistry>()));
        services.AddSingleton(sp => new Trade(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<GoodsRegistry>()));
        services.AddSingleton(sp => new PlanetController(sp.GetRequiredService<GameState>()));

        // Every mission is a plugin, including the two the game has always had; MEF finds them in the Missions folder.
        services.AddSingleton(sp => new MissionRegistry(
            MissionLoader.LoadFrom(
                AppContext.BaseDirectory,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(MissionLoader))),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<MissionRegistry>()));

        // The one place the game and its missions talk to each other.
        services.AddSingleton(sp => new MissionRunner(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<Trade>(),
            sp.GetRequiredService<MissionRegistry>(),
            sp.GetRequiredService<PlanetController>()));
    }

    private static void AddEliteSimulation(this IServiceCollection services)
    {
        services.AddSingleton(sp => new Universe(sp.GetRequiredService<IShipFactory>(), sp.GetRequiredService<RNG>()));
        services.AddSingleton(sp => new Stars(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<IEliteDraw>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<RenditionRegistry>().Starfield));
        services.AddSingleton(sp =>
        {
            SoundConfigSettings config = sp.GetRequiredService<GameState>().Config.Engine.Sound;
            return new AudioController(
                sp.GetRequiredService<ISound>(),
                BuildEliteSfx(),
                new() { MusicOn = config.Music, EffectsOn = config.Effects },
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<AudioController>());
        });
        services.AddSingleton(sp => new Pilot(
            sp.GetRequiredService<IEliteDraw>(),
            sp.GetRequiredService<AudioController>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<GameState>()));
        services.AddSingleton(sp => new Combat(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<AudioController>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<Trade>(),
            sp.GetRequiredService<Pilot>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<IEliteDraw>(),
            sp.GetRequiredService<IRendition>(),
            sp.GetRequiredService<IShipFactory>(),
            sp.GetRequiredService<RNG>(),
            sp.GetRequiredService<MissionRunner>(),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<Combat>()));
        services.AddSingleton(sp => new SaveFile(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<Trade>(),
            sp.GetRequiredService<PlanetController>(),
            sp.GetRequiredService<MissionRegistry>(),
            sp.GetRequiredService<ConfigFile<EliteConfig>>().BaseDirectory,
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<SaveFile>()));
        services.AddSingleton(sp => new Space(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<AudioController>(),
            sp.GetRequiredService<Pilot>(),
            sp.GetRequiredService<Combat>(),
            sp.GetRequiredService<Trade>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<PlanetController>(),
            sp.GetRequiredService<Stars>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<IEliteDraw>(),
            sp.GetRequiredService<IRendition>(),
            sp.GetRequiredService<RNG>(),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<Space>()));
        services.AddSingleton(sp => new ScannerController(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<PlayerShip>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<Combat>(),
            sp.GetRequiredService<IView<ScannerModel>>()));
    }

    private static void PopulateScreens(IServiceProvider sp)
    {
        ScreenManager<Screen, IScreenController> views = sp.GetRequiredService<ScreenManager<Screen, IScreenController>>();
        views.Add(Screen.IntroOne, sp.GetRequiredService<Intro1Controller>());
        views.Add(Screen.IntroTwo, sp.GetRequiredService<Intro2Controller>());
        views.Add(Screen.GalacticChart, sp.GetRequiredService<GalacticChartController>());
        views.Add(Screen.ShortRangeChart, sp.GetRequiredService<ShortRangeChartController>());
        views.Add(Screen.PlanetData, sp.GetRequiredService<PlanetDataController>());
        views.Add(Screen.MarketPrices, sp.GetRequiredService<MarketController>());
        views.Add(Screen.CommanderStatus, sp.GetRequiredService<CommanderStatusController>());
        views.Add(Screen.FrontView, CreatePilotController(sp, PilotDirection.Front));
        views.Add(Screen.RearView, CreatePilotController(sp, PilotDirection.Rear));
        views.Add(Screen.LeftView, CreatePilotController(sp, PilotDirection.Left));
        views.Add(Screen.RightView, CreatePilotController(sp, PilotDirection.Right));
        views.Add(Screen.Docking, sp.GetRequiredService<DockingView>());
        views.Add(Screen.Undocking, sp.GetRequiredService<LaunchView>());
        views.Add(Screen.Hyperspace, sp.GetRequiredService<HyperspaceView>());
        views.Add(Screen.Inventory, sp.GetRequiredService<InventoryController>());
        views.Add(Screen.EquipShip, sp.GetRequiredService<EquipmentController>());
        views.Add(Screen.Options, sp.GetRequiredService<OptionsController>());
        views.Add(Screen.Credits, sp.GetRequiredService<CreditsController>());
        views.Add(Screen.LoadCommander, sp.GetRequiredService<LoadCommanderController>());
        views.Add(Screen.SaveCommander, sp.GetRequiredService<SaveCommanderController>());
        views.Add(Screen.Quit, sp.GetRequiredService<QuitController>());
        views.Add(Screen.Settings, sp.GetRequiredService<SettingsController>());
        views.Add(Screen.EngineSettings, sp.GetRequiredService<EngineSettingsController>());
        views.Add(Screen.MissionBriefing, sp.GetRequiredService<MissionBriefingController>());
        views.Add(Screen.EscapeCapsule, sp.GetRequiredService<EscapeCapsuleController>());
        views.Add(Screen.GameOver, sp.GetRequiredService<GameOverController>());
    }

    // The four cockpit windows share one PilotController, differing only in facing, so PopulateScreens constructs each directly.
    private static PilotController CreatePilotController(IServiceProvider sp, PilotDirection direction) => new(
        sp.GetRequiredService<GameState>(),
        sp.GetRequiredService<EliteControlMap>(),
        sp.GetRequiredService<IKeyboard>(),
        sp.GetRequiredService<Pilot>(),
        sp.GetRequiredService<PlayerShip>(),
        sp.GetRequiredService<Stars>(),
        sp.GetRequiredService<Space>(),
        sp.GetRequiredService<Combat>(),
        direction,
        sp.GetRequiredService<IEliteDraw>(),
        sp.GetRequiredService<IView<PilotModel>>());

    // The ~25 views EliteMain used to construct itself, now registered so
    // AddEliteMain's screen-map factory above can resolve them.
    private static void AddEliteViews(this IServiceCollection services)
    {
        services.AddRendition();
        services.AddBaseView();
        services.AddEliteFlightViews();
        services.AddSplitScreens();
    }

    private static void AddEliteFlightViews(this IServiceCollection services)
    {
        services.AddSingleton(sp => new DockingView(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<AudioController>(),
            sp.GetRequiredService<Space>(),
            sp.GetRequiredService<Combat>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<IEliteDraw>()));
        services.AddSingleton(sp => new LaunchView(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<AudioController>(),
            sp.GetRequiredService<Space>(),
            sp.GetRequiredService<Combat>(),
            sp.GetRequiredService<Universe>(),
            sp.GetRequiredService<IEliteDraw>()));
        services.AddSingleton(sp => new HyperspaceView(
            sp.GetRequiredService<GameState>(),
            sp.GetRequiredService<AudioController>(),
            sp.GetRequiredService<IEliteDraw>()));
    }

    // The tier's shared chrome: frame rate, info message, hyperspace countdown and tier-split screen headers.
    private static void AddBaseView(this IServiceCollection services)
    {
        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().BaseView);

        // Registered with the simulation, not the screens: EliteMain and equip-ship both refresh it directly.
        services.AddSingleton(sp => sp.GetRequiredService<RenditionRegistry>().View<ScannerModel>());
    }

    // What the configured rendition drew, checked against the game's screens, narrowed to IViewSurface's three members.
    private static void AddRendition(this IServiceCollection services)
        => services.AddSingleton(sp => new RenditionRegistry(
            sp.GetRequiredService<IRendition>(),
            sp.GetRequiredService<IEliteDraw>()));

    // TODO: improve this (moved from EliteMain, see backlog)
    private static Dictionary<string, SfxSample> BuildEliteSfx() => new()
    {
        { nameof(SoundEffect.Launch), new(32) },
        { nameof(SoundEffect.Crash), new(7) },
        { nameof(SoundEffect.Dock), new(36) },
        { nameof(SoundEffect.Gameover), new(24) },
        { nameof(SoundEffect.Pulse), new(4) },
        { nameof(SoundEffect.HitEnemy), new(4) },
        { nameof(SoundEffect.Explode), new(23) },
        { nameof(SoundEffect.Ecm), new(23) },
        { nameof(SoundEffect.Missile), new(25) },
        { nameof(SoundEffect.Hyperspace), new(37) },
        { nameof(SoundEffect.IncomingFire1), new(4) },
        { nameof(SoundEffect.IncomingFire2), new(5) },
        { nameof(SoundEffect.Beep), new(2) },
        { nameof(SoundEffect.Boop), new(7) },
    };
}

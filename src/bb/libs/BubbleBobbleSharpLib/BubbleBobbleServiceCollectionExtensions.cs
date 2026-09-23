// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Renditions;
using BubbleBobbleSharpLib.Config;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Renditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpKind;
using SharpKind.Abstraction;
using SharpKind.Abstraction.Config;
using SharpKind.Assets;
using SharpKind.Audio;
using SharpKind.Config;

namespace BubbleBobbleSharpLib;

public static class BubbleBobbleServiceCollectionExtensions
{
    private const string ConfigFileName = "bubblebobble.sharp";

    // BbConfig is internal, so Main cannot construct a ConfigFile<BbConfig>; this registers it
    // from inside the assembly that can, exposing only the public AudioOptions.
    public static IServiceCollection AddBbConfig(this IServiceCollection services, string userDataPath)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(sp => new ConfigFile<BbConfig>(
            userDataPath,
            ConfigFileName,
            RepairConfig,
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConfigFile<BbConfig>>()));
        services.AddSingleton(sp =>
        {
            BbConfig config = sp.GetRequiredService<ConfigFile<BbConfig>>().ReadConfig();
            return new AudioOptions { MusicOn = config.Engine.Sound.Music, EffectsOn = config.Engine.Sound.Effects };
        });
        return services;
    }

    // The engine half of the internal BbConfig, so Main can read the backend and the rendition
    // before the container exists.
    public static EngineConfigSettings ReadEngineSettings(string userDataPath, ILoggerFactory loggerFactory)
        => EngineConfigReader.Read<BbConfig>(userDataPath, ConfigFileName, RepairConfig, loggerFactory);

    // Loaded before the container, because the window is made at the size the rendition draws at.
    public static InstalledRenditions LoadRendition(string name, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return RenditionLoader.LoadFrom(
            AppContext.BaseDirectory,
            name,
            loggerFactory.CreateLogger(typeof(RenditionLoader)));
    }

    // Over the locator AddGameEngine registered, which knows only the executable's own Assets
    // folder. This game keeps none of its own.
    public static IServiceCollection AddBbRenditionAssets(this IServiceCollection services, InstalledRenditions renditions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(renditions);

        services.AddSingleton<IAssetLocator>(
            _ => AssetLocator.CreateFrom(renditions.Folder, renditions.Chosen.Name));

        // The game draws through whichever rendition was loaded, so it is a service like any other.
        services.AddSingleton(renditions);
        services.AddSingleton(renditions.Chosen);

        // The levels sit beside the rendition's artwork rather than in the manifest: the manifest
        // names images, fonts and a palette, and a hundred levels are none of those.
        services.AddSingleton(_ => LevelStore.Read(LevelPath(renditions)));

        // A sibling file rather than a field on Level: Phase 2 kept zone-data.txt's export apart
        // from levels.txt's, so that the port reads rectangles and nothing else, and SolidMap is
        // the one place the two are brought together.
        services.AddSingleton(_ => ZoneStore.Read(ZonePath(renditions)));

        return services;
    }

    // <rendition folder>/Assets/Levels/levels.json, the same layout AssetLocator resolves against.
    public static string LevelPath(InstalledRenditions renditions)
    {
        ArgumentNullException.ThrowIfNull(renditions);

        return Path.Combine(renditions.Folder, "Assets", "Levels", "levels.json");
    }

    // <rendition folder>/Assets/Levels/zones.json, beside levels.json.
    public static string ZonePath(InstalledRenditions renditions)
    {
        ArgumentNullException.ThrowIfNull(renditions);

        return Path.Combine(renditions.Folder, "Assets", "Levels", "zones.json");
    }

    // $E9EA's generator. Random.Shared stands in for the CIA timer byte it mixes in, the one part of
    // it that is hardware; the rest is BbRandom's own.
    public static IServiceCollection AddBbRandom(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(_ => new BbRandom(new RandomSource(Random.Shared)));
        return services;
    }

    // The composition root asks for the game, not the pieces it is built from.
    public static IServiceCollection AddBbMain(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(sp => new BubbleBobbleMain(
            sp.GetRequiredService<IAbstraction>(),
            sp.GetRequiredService<IAssetLocator>(),
            sp.GetRequiredService<IBbRendition>(),
            sp.GetRequiredService<LevelStore>(),
            sp.GetRequiredService<ZoneStore>(),
            sp.GetRequiredService<AudioOptions>()));
        services.AddSingleton<IGame>(sp => sp.GetRequiredService<BubbleBobbleMain>());
        services.AddSingleton<IGameApp>(sp => sp.GetRequiredService<BubbleBobbleMain>());
        return services;
    }

    // No game settings yet, so this is the shared engine repair plus BbConfig's own defaults.
    internal static bool RepairConfig(BbConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return config.Repair();
    }
}

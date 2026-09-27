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

    public static EngineConfigSettings ReadEngineSettings(string userDataPath, ILoggerFactory loggerFactory)
        => EngineConfigReader.Read<BbConfig>(userDataPath, ConfigFileName, RepairConfig, loggerFactory);

    public static InstalledRenditions LoadRendition(string name, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return RenditionLoader.LoadFrom(
            AppContext.BaseDirectory,
            name,
            loggerFactory.CreateLogger(typeof(RenditionLoader)));
    }

    public static IServiceCollection AddBbRenditionAssets(this IServiceCollection services, InstalledRenditions renditions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(renditions);

        services.AddSingleton<IAssetLocator>(
            _ => AssetLocator.CreateFrom(renditions.Folder, renditions.Chosen.Name));

        services.AddSingleton(renditions);
        services.AddSingleton(renditions.Chosen);

        services.AddSingleton(_ => LevelStore.Read(LevelPath(renditions)));

        services.AddSingleton(_ => ZoneStore.Read(ZonePath(renditions)));

        return services;
    }

    public static string LevelPath(InstalledRenditions renditions)
    {
        ArgumentNullException.ThrowIfNull(renditions);

        return Path.Combine(renditions.Folder, "Assets", "Levels", "levels.json");
    }

    public static string ZonePath(InstalledRenditions renditions)
    {
        ArgumentNullException.ThrowIfNull(renditions);

        return Path.Combine(renditions.Folder, "Assets", "Levels", "zones.json");
    }

    public static IServiceCollection AddBbRandom(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(_ => new BbRandom(new RandomSource(Random.Shared)));
        return services;
    }

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

    internal static bool RepairConfig(BbConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return config.Repair();
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Config;
using BubbleBobbleSharpLib.Renditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        return services.AddSingleton<IAssetLocator>(
            _ => AssetLocator.CreateFrom(renditions.Folder, renditions.Chosen.Name));
    }

    // The composition root asks for the game, not the pieces it is built from.
    public static IServiceCollection AddBbMain(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(sp => new BubbleBobbleMain(
            sp.GetRequiredService<IAbstraction>(),
            sp.GetRequiredService<IAssetLocator>(),
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

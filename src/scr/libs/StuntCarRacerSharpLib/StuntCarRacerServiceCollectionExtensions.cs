// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpKind;
using SharpKind.Abstraction;
using SharpKind.Abstraction.Config;
using SharpKind.Assets;
using SharpKind.Audio;
using SharpKind.Config;
using StuntCarRacerSharpLib.Config;

namespace StuntCarRacerSharpLib;

public static class StuntCarRacerServiceCollectionExtensions
{
    private const string ConfigFileName = "stuntcarracer.sharp";

    // ScrConfig is internal, so Program.Main can't construct a ConfigFile<ScrConfig> directly; this registers it from inside the
    // assembly that can, exposing only the already-public AudioOptions.
    public static IServiceCollection AddScrConfig(this IServiceCollection services, string userDataPath)
    {
        services.AddSingleton(sp => new ConfigFile<ScrConfig>(
            userDataPath,
            ConfigFileName,
            RepairConfig,
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConfigFile<ScrConfig>>()));
        services.AddSingleton(sp =>
        {
            ScrConfig config = sp.GetRequiredService<ConfigFile<ScrConfig>>().ReadConfig();
            return new AudioOptions { MusicOn = config.Engine.Sound.Music, EffectsOn = config.Engine.Sound.Effects };
        });
        return services;
    }

    // Exposes public engine settings from internal ScrConfig, so Program.Main can read backend/tier/window scale before the DI
    // container exists; it needs SharpKind.SDL to pick an abstraction, a dependency this library deliberately does not have.
    public static EngineConfigSettings ReadEngineSettings(string userDataPath, ILoggerFactory loggerFactory)
        => EngineConfigReader.Read<ScrConfig>(userDataPath, ConfigFileName, RepairConfig, loggerFactory);

    // Single shared source of entropy: unseeded Random in production, replaceable with a seeded one in tests via RandomSource's constructor seam.
    public static IServiceCollection AddScrRandom(this IServiceCollection services)
    {
        services.AddSingleton(_ => Random.Shared);
        services.AddSingleton<IRandomSource>(sp => new RandomSource(sp.GetRequiredService<Random>()));
        return services;
    }

    // Registers the game itself, as AddEliteMain does for Elite: the composition root asks for the game, not its pieces.
    public static IServiceCollection AddScrMain(this IServiceCollection services)
    {
        services.AddSingleton(sp => new StuntCarRacerMain(
            sp.GetRequiredService<IAbstraction>(),
            sp.GetRequiredService<IAssetLocator>(),
            sp.GetRequiredService<AudioOptions>(),
            sp.GetRequiredService<IRandomSource>()));
        services.AddSingleton<IGame>(sp => sp.GetRequiredService<StuntCarRacerMain>());
        services.AddSingleton<IGameApp>(sp => sp.GetRequiredService<StuntCarRacerMain>());
        return services;
    }

    // Stunt Car Racer has no settings of its own yet, so this is just the shared engine repair.
    internal static bool RepairConfig(ScrConfig config) => config.Repair();
}

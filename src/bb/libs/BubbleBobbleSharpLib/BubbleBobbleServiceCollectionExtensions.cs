// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Config;
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

    // BbConfig is internal, so Program.Main can't reference or construct a
    // ConfigFile<BbConfig> directly; this registers it from inside the
    // assembly that can, exposing only the already-public AudioOptions that
    // BubbleBobbleMain's constructor accepts.
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

    // Exposes the (public) engine settings from the (internal) BbConfig, so
    // Program.Main - which picks between SoftwareAbstraction and SDLAbstraction
    // and therefore needs to reference SharpKind.SDL, a dependency
    // BubbleBobbleSharpLib itself deliberately does not have - can read the
    // backend and the window scale before the DI container exists.
    public static EngineConfigSettings ReadEngineSettings(string userDataPath, ILoggerFactory loggerFactory)
        => EngineConfigReader.Read<BbConfig>(userDataPath, ConfigFileName, RepairConfig, loggerFactory);

    // Registers the game itself: the composition root asks for the game, not
    // for the pieces it is built from.
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

    // Bubble Bobble has no settings of its own yet, so this is the shared
    // engine repair.
    internal static bool RepairConfig(BbConfig config) => config.Repair();
}

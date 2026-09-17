// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpKind.Abstraction;
using SharpKind.Abstraction.Config;
using SharpKind.Assets;
using SharpKind.SDL;

namespace SharpKind.App;

/// <summary>
/// The registrations every game's composition root makes before its own: the
/// abstraction the engine settings select, the graphics, sound and keyboard it
/// exposes, and the asset locator fixed to the configured tier.
/// </summary>
public static class GameServiceCollectionExtensions
{
    private const string AssetLogCategory = "Assets";

    /// <summary>
    /// Registers the engine services shared by every game.
    /// </summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="engine">The engine settings, already read and repaired.</param>
    /// <param name="screenWidth">The native render width, before <see cref="EngineConfigSettings.WindowScale"/>.</param>
    /// <param name="screenHeight">The native render height, before <see cref="EngineConfigSettings.WindowScale"/>.</param>
    /// <param name="title">The window title.</param>
    /// <param name="loggerFactory">The app's logger factory, registered for the graph below to resolve.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddGameEngine(
        this IServiceCollection services,
        EngineConfigSettings engine,
        int screenWidth,
        int screenHeight,
        string title,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(engine);

        services.AddSingleton(loggerFactory);

        // The two backends take the same arguments and expose the same abstraction, so the config only decides which one is constructed.
        services.AddSingleton<IAbstraction>(sp => engine.Backend == Backend.Hardware
            ? new SDLAbstraction(
                screenWidth,
                screenHeight,
                engine.WindowScale ?? 1,
                title,
                sp.GetRequiredService<IAssetLocator>(),
                engine.Graphics.FontKind,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger(AssetLogCategory))
            : new SoftwareAbstraction(
                screenWidth,
                screenHeight,
                engine.WindowScale ?? 1,
                title,
                sp.GetRequiredService<IAssetLocator>(),
                engine.Graphics.FontKind,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger(AssetLogCategory)));

        services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Graphics);
        services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Layout);
        services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Sound);
        services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Keyboard);
        services.AddSingleton(sp => sp.GetRequiredService<IAbstraction>().Gamepad);
        services.AddSingleton<IAssetLocator>(_ => AssetLocator.Create(engine.Rendition));

        return services;
    }
}

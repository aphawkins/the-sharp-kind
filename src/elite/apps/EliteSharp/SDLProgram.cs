// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Renditions;
using EliteSharpLib;
using EliteSharpLib.Renditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpKind.Abstraction.Config;
using SharpKind.App;
using SharpKind.SDL;

[assembly: CLSCompliant(false)]

namespace EliteSharp;

internal static class SDLProgram
{
    private const string Title = "Elite - The Sharp Kind";

    public static int Main()
        => GameApp.Run(
            Title,
            logFileName: "elite-.log",
            logLevelEnvironmentVariable: "ELITE_LOG_LEVEL",
            EliteServiceCollectionExtensions.ReadEngineSettings,
            BuildServices,
            SDLMessageBox.ShowError);

    private static ServiceCollection BuildServices(string userDataPath, ILoggerFactory loggerFactory, EngineConfigSettings engine)
    {
        // The rendition sets the screen size, so it must load before the window is created.
        InstalledRenditions renditions = EliteServiceCollectionExtensions.LoadRendition(engine.Rendition, loggerFactory);

        // Only the rendition knows which magnifications are valid, so scale is resolved after it loads.
        engine.WindowScale = WindowScales.Resolve(renditions.Chosen, engine.WindowScale);

        ServiceCollection services = new();
        services.AddGameEngine(engine, renditions.Chosen.ScreenWidth, renditions.Chosen.ScreenHeight, Title, loggerFactory);
        services.AddEliteConfig(userDataPath);
        services.AddEliteControls(userDataPath);
        services.AddRenditionAssets(renditions);
        services.AddEliteMain(renditions);

        return services;
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib;
using BubbleBobbleSharpLib.Renditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpKind.Abstraction.Config;
using SharpKind.App;
using SharpKind.SDL;

[assembly: CLSCompliant(false)]

namespace BubbleBobbleSharp;

internal static class SDLProgram
{
    private const string Title = "Bubble Bobble - The Sharp Kind";

    public static int Main()
        => GameApp.Run(
            Title,
            logFileName: "bb-.log",
            logLevelEnvironmentVariable: "BB_LOG_LEVEL",
            BubbleBobbleServiceCollectionExtensions.ReadEngineSettings,
            BuildServices,
            SDLMessageBox.ShowError);

    private static ServiceCollection BuildServices(string userDataPath, ILoggerFactory loggerFactory, EngineConfigSettings engine)
    {
        // First, because it says what size the game draws at and the window is made at that
        // size - so the artwork and the resolution cannot disagree.
        InstalledRenditions renditions = BubbleBobbleServiceCollectionExtensions.LoadRendition(
            engine.Rendition,
            loggerFactory);

        // Which magnifications are on offer is the rendition's, so the scale is settled only now.
        engine.WindowScale = ResolveWindowScale(renditions, engine.WindowScale);

        ServiceCollection services = new();
        services.AddGameEngine(
            engine,
            renditions.Chosen.ScreenWidth,
            renditions.Chosen.ScreenHeight,
            Title,
            loggerFactory);
        services.AddBbConfig(userDataPath);
        services.AddBbRenditionAssets(renditions);
        services.AddBbMain();

        return services;
    }

    private static int ResolveWindowScale(InstalledRenditions renditions, int? configured)
    {
        if (configured is null)
        {
            return renditions.Chosen.DefaultWindowScale;
        }

        // Pegged to the nearest offered rather than rejected - it is a wish about window size.
        return renditions.Chosen.WindowScales
            .OrderBy(scale => Math.Abs(scale - configured.Value))
            .First();
    }
}

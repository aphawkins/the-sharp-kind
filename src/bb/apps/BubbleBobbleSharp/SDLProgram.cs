// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib;
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

    // The C64's screen: 40x25 characters of 8x8 pixels. The playfield is the
    // middle 32 columns, with the sidebars either side of it.
    private const int ScreenWidth = 320;
    private const int ScreenHeight = 200;

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
        ServiceCollection services = new();
        services.AddGameEngine(engine, ScreenWidth, ScreenHeight, Title, loggerFactory);
        services.AddBbConfig(userDataPath);
        services.AddBbMain();

        return services;
    }
}

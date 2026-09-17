// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpKind.Abstraction;
using SharpKind.Abstraction.Config;
using SharpKind.App;
using SharpKind.Graphics;
using SharpKind.SDL;

[assembly: CLSCompliant(false)]

namespace SharpKind.UI.Gallery;

/// <summary>
/// Opens a window showing every control and lets the keyboard drive the ones
/// that take input.
/// <para>
/// Built on <see cref="GameApp"/> like the two games, rather than standing a
/// window up for itself: the gallery then gets the same backend choice, the
/// same window scaling, the same logging and the same failure reporting they
/// do - and, through <c>GameHost</c>, the <c>GAME_KEY_SCRIPT</c> and
/// <c>GAME_FRAME_DUMP_DIR</c> facilities, which are how a control can be
/// driven and photographed without injecting keys at the OS.
/// </para>
/// </summary>
internal static class Program
{
    private const string Title = "SharpKind UI - Gallery";
    private const string ConfigFileName = "gallery.sharp";

    // 8-bit tier's canvas width. Height is in rows, not pixels: the gallery lays out a row at a
    // time, and row height follows the backend's actual font (8px bitmap vs ~2x that for the
    // hardware backend's 12pt TrueType face) - a fixed pixel canvas would crop one of them.
    private const int Width = 320;
    private const int BitmapRowHeight = 8;

    // Rounded up, not down: slack at the foot costs empty pixels, but a shortfall would cost a whole row.
    private const int TrueTypeRowHeight = 18;

    // Keeps the window on a laptop screen: a taller-row canvas magnified as far as a short one would go off the bottom.
    private const int MaxWindowHeight = 900;

    public static int Main()
        => GameApp.Run(
            Title,
            logFileName: "gallery-.log",
            logLevelEnvironmentVariable: "GALLERY_LOG_LEVEL",
            ReadEngineSettings,
            BuildServices,
            SDLMessageBox.ShowError);

    private static EngineConfigSettings ReadEngineSettings(string userDataPath, ILoggerFactory loggerFactory)
        => EngineConfigReader.Read<GalleryConfig>(userDataPath, ConfigFileName, RepairConfig, loggerFactory);

    // The engine settings repair themselves; the gallery adds nothing that
    // could need it.
    private static bool RepairConfig(GalleryConfig config) => config.Repair();

    private static ServiceCollection BuildServices(
        string userDataPath,
        ILoggerFactory loggerFactory,
        EngineConfigSettings engine)
    {
        // Row height follows the font kind, not the backend, since both backends draw text the same way now.
        int rowHeight = engine.Graphics.FontKind == FontKind.TrueType ? TrueTypeRowHeight : BitmapRowHeight;
        int height = Gallery.LayoutRows * rowHeight;

        // An unchosen scale is the gallery's own default; from there it shrinks
        // to whatever fits.
        int scale = engine.WindowScale ?? GalleryConfig.DefaultWindowScale;

        while (scale > 1 && height * scale > MaxWindowHeight)
        {
            scale--;
        }

        engine.WindowScale = scale;

        ServiceCollection services = new();
        services.AddGameEngine(engine, Width, height, Title, loggerFactory);
        services.AddSingleton(engine);
        services.AddSingleton<IGameApp>(sp => new GalleryMain(
            sp.GetRequiredService<IAbstraction>(),
            sp.GetRequiredService<EngineConfigSettings>()));

        return services;
    }
}

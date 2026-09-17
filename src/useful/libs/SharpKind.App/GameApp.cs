// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using SharpKind.Abstraction;
using SharpKind.Abstraction.Config;

namespace SharpKind.App;

/// <summary>
/// The shared half of a game executable's <c>Program.Main</c>: resolve the
/// user-data directory, stand up logging, build the container, run the game,
/// and report a failure in a way a player can act on.
/// </summary>
/// <remarks>
/// This is app-layer policy, not library code - it is why <c>SharpKind.App</c>
/// exists as an assembly no game library references. Each executable supplies
/// only what actually differs between the games: its title, its log file, its
/// log-level environment variable, and its own service registrations.
/// </remarks>
public static class GameApp
{
    /// <summary>
    /// Runs a game to completion and returns the process exit code: zero for a
    /// normal exit, non-zero if the user-data directory could not be resolved
    /// or the game terminated unexpectedly. Returning the code rather than
    /// calling <see cref="Environment.Exit(int)"/> lets <c>Main</c> unwind, so
    /// the container disposes its singletons on the way out.
    /// </summary>
    /// <param name="title">The window/log title, as logged at startup.</param>
    /// <param name="logFileName">
    /// The rolling log file's name within the user-data <c>logs</c> directory;
    /// Serilog inserts the date before the extension, so this is a template
    /// like <c>elite-.log</c> rather than a literal filename.
    /// </param>
    /// <param name="logLevelEnvironmentVariable">
    /// Names the environment variable that raises or lowers the minimum log
    /// level, overriding both the default and whatever the config file says -
    /// the escape hatch for when the config file itself is what needs
    /// debugging.
    /// </param>
    /// <param name="readEngineSettings">
    /// Reads the game's config file and returns its engine half, given the
    /// user-data path and a logger for the read itself. Called before the
    /// real logger exists (it has no file-retention setting to honour yet),
    /// so it gets a console-only bootstrap logger.
    /// </param>
    /// <param name="buildServices">
    /// The game's own composition, given the user-data path, the real
    /// logger factory and the already-read engine settings. It must register
    /// an <see cref="IGameApp"/>.
    /// </param>
    /// <param name="reportFailure">
    /// How a failure reaches a player with no console to read, given the title
    /// and the message. Both games pass
    /// <see cref="SDL.SDLMessageBox.ShowError"/>; it is a parameter rather than
    /// that call directly so a test can watch what a failure reports without
    /// opening a real dialog and waiting for somebody to dismiss it.
    /// </param>
    /// <returns>The process exit code.</returns>
    public static int Run(
        string title,
        string logFileName,
        string logLevelEnvironmentVariable,
        Func<string, ILoggerFactory, EngineConfigSettings> readEngineSettings,
        Func<string, ILoggerFactory, EngineConfigSettings, ServiceCollection> buildServices,
        Action<string, string> reportFailure)
    {
        ArgumentNullException.ThrowIfNull(readEngineSettings);
        ArgumentNullException.ThrowIfNull(buildServices);
        ArgumentNullException.ThrowIfNull(reportFailure);

        if (!AppStartup.TryResolveUserDataPath(out string userDataPath))
        {
            // TryResolveUserDataPath has already reported why to stderr and to
            // the fallback startup log; there is nowhere to write a real log.
            return 1;
        }

        LogEventLevel? environmentLevel = ReadEnvironmentLevel(logLevelEnvironmentVariable);
        EngineConfigSettings engine = ReadEngineSettings(userDataPath, environmentLevel, readEngineSettings);

        LogEventLevel minimumLevel = environmentLevel ?? LevelConvert.ToSerilogLevel(engine.Logging.MinimumLevel);

        using Logger seriLogger = CreateSeriLogger(userDataPath, logFileName, minimumLevel, engine.Logging.RetainedFileCount);
        using LoggerFactory loggerFactory = new();
        loggerFactory.AddSerilog(seriLogger);

        Microsoft.Extensions.Logging.ILogger logger = loggerFactory.CreateLogger(nameof(GameApp));

        // Logged before composition, so a startup failure still leaves behind which build and settings were tried.
        LogMessages.StartingTitle(logger, title);
        LogStartupDiagnostics(logger, engine);

        try
        {
            // Composition sits inside the try: it's where startup failures actually happen (rendition, assets, plugins).
            using ServiceProvider provider = buildServices(userDataPath, loggerFactory, engine).BuildServiceProvider();
            IGameApp game = provider.GetRequiredService<IGameApp>();
            game.Run();
        }
        catch (Exception ex)
        {
            // Logged in full above, so the player gets a hint and a non-zero exit rather than a raw stack dump.
            LogMessages.CriticalAppTerminated(logger, ex);
            AppStartup.WriteFailureHint(ex, userDataPath);

            // stderr is not enough on its own: a game started from a shortcut
            // has no console to print it to.
            reportFailure(title, AppStartup.DescribeFailure(ex, userDataPath));
            return -1;
        }

        return 0;
    }

    // Carries what a bug report rarely comes with: build, OS/runtime, and engine settings. Logged
    // as JSON so it can be machine-parsed back out.
    private static void LogStartupDiagnostics(Microsoft.Extensions.Logging.ILogger logger, EngineConfigSettings engine)
    {
        string version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? "unknown";

        var systemInfo = new
        {
            version,
            os = RuntimeInformation.OSDescription,
            runtime = RuntimeInformation.FrameworkDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            processorCount = Environment.ProcessorCount,
        };
        string systemInfoJson = JsonSerializer.Serialize(systemInfo);
        LogMessages.SystemInfo(logger, systemInfoJson);

        var engineSettings = new
        {
            backend = engine.Backend.ToString(),
            rendition = engine.Rendition,
            windowScale = engine.WindowScale,
            fps = engine.Graphics.Fps,
            graphicStyle = engine.Graphics.FillMode.ToString(),
            depthSort = engine.Graphics.DepthSort.ToString(),
            fontKind = engine.Graphics.FontKind.ToString(),
            soundEffects = engine.Sound.Effects,
            soundMusic = engine.Sound.Music,
        };
        string engineSettingsJson = JsonSerializer.Serialize(engineSettings);
        LogMessages.EngineSettings(logger, engineSettingsJson);
    }

    // Null means unset or unparseable; the caller falls back to the config value.
    private static LogEventLevel? ReadEnvironmentLevel(string logLevelEnvironmentVariable)
        => Enum.TryParse(
            Environment.GetEnvironmentVariable(logLevelEnvironmentVariable),
            ignoreCase: true,
            out LogEventLevel envLevel)
            ? envLevel
            : null;

    // Reading the config needs a logger, which can't yet know the config's own retention setting.
    // A console-only bootstrap logger breaks the cycle: it needs no retention setting and its level is already known.
    private static EngineConfigSettings ReadEngineSettings(
        string userDataPath,
        LogEventLevel? environmentLevel,
        Func<string, ILoggerFactory, EngineConfigSettings> readEngineSettings)
    {
        using Logger bootstrapLogger = CreateBootstrapLogger(environmentLevel ?? LogEventLevel.Information);
        using LoggerFactory bootstrapLoggerFactory = new();
        bootstrapLoggerFactory.AddSerilog(bootstrapLogger);

        return readEngineSettings(userDataPath, bootstrapLoggerFactory);
    }

    private static Logger CreateBootstrapLogger(LogEventLevel minimumLevel)
        => new LoggerConfiguration()
            .Enrich
            .FromLogContext()
            .MinimumLevel
            .Is(minimumLevel)
            .WriteTo
            .Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}",
                formatProvider: System.Globalization.CultureInfo.InvariantCulture)
            .CreateLogger();

    private static Logger CreateSeriLogger(string userDataPath, string logFileName, LogEventLevel minimumLevel, int retainedFileCount)
        => new LoggerConfiguration()
            .Enrich
            .FromLogContext()
            .MinimumLevel
            .Is(minimumLevel)
            .WriteTo
            .Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}",
                formatProvider: System.Globalization.CultureInfo.InvariantCulture)
            .WriteTo
            .File(
                Path.Combine(userDataPath, "logs", logFileName),
                formatProvider: System.Globalization.CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedFileCount)
            .CreateLogger();
}

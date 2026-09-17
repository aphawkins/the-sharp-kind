// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind;

/// <summary>
/// Shared console-app startup helpers: resolving the per-user data directory and reporting
/// startup/crash failures in a way that's actionable rather than a raw stack dump.
/// </summary>
public static class AppStartup
{
    private const string AppDataDirName = "The Sharp Kind";

    /// <summary>
    /// Resolves the shared per-user data directory (e.g. <c>%AppData%\The Sharp Kind</c> on Windows,
    /// <c>~/.config/The Sharp Kind</c> on Linux). Returns <see langword="false"/>, prints a diagnostic
    /// to stderr, and appends a fallback startup log if it cannot be resolved to an absolute path -
    /// most commonly because the HOME environment variable is unset on Linux.
    /// </summary>
    public static bool TryResolveUserDataPath(out string userDataPath)
    {
        userDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataDirName);

        if (Path.IsPathRooted(userDataPath))
        {
            return true;
        }

        const string message = "Could not determine the user data directory (resolved to a relative path); "
            + "the HOME environment variable is likely not set.";

        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Set HOME and try again.");
        WriteFallbackStartupLog(message);
        return false;
    }

    /// <summary>
    /// Prints an actionable hint for an unhandled startup/runtime failure. Full exception details
    /// (including the stack trace) are expected to already have been logged via the app's normal
    /// logger before calling this; it only improves what the user sees on the console.
    /// </summary>
    public static void WriteFailureHint(Exception ex, string userDataPath)
        => Console.Error.WriteLine(DescribeFailure(ex, userDataPath));

    /// <summary>
    /// The player-facing account of an unhandled startup or runtime failure: what went wrong,
    /// and where the rest of it is written down. Separate from <see cref="WriteFailureHint"/>
    /// because the console is not the only place this has to reach - a game started from a
    /// shortcut has no console to read - and the two must not drift apart.
    /// </summary>
    /// <param name="ex">The failure to describe.</param>
    /// <param name="userDataPath">Where the log directory sits, so the text can name it.</param>
    /// <returns>The text to show, over several lines.</returns>
    public static string DescribeFailure(Exception ex, string userDataPath)
    {
        ArgumentNullException.ThrowIfNull(ex);

        string cause = ex is DllNotFoundException

            // SDL3 natives ship inside the ppy.SDL3*-CS NuGet packages, so this means an unsupported platform/architecture.
            ? "A required native library could not be loaded."
                + Environment.NewLine
                + "This usually means the current platform/architecture isn't supported."
            : "Application terminated unexpectedly."
                + Environment.NewLine
                + ex.Message;

        return cause
            + Environment.NewLine
            + $"See the log file under {Path.Combine(userDataPath, "logs")} for full details.";
    }

    private static void WriteFallbackStartupLog(string message)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(Path.GetTempPath(), $"{AppDataDirName}-startup-error.log"),
                $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort fallback logging only; nothing more useful to do if this fails too.
        }
    }
}

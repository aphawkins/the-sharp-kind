// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Microsoft.Extensions.Logging;

namespace SharpKind.Abstraction.Config;

/// <summary>
/// The engine's logging settings, stored under the config file's
/// <c>engine.logging</c> element.
/// </summary>
public sealed class LoggingConfigSettings
{
    // A year of daily files is generous for a desktop game; past it is a
    // typo rather than an intention.
    private const int MaxRetainedFileCount = 366;

    private const int DefaultRetainedFileCount = 7;

    private const LogLevel DefaultMinimumLevel = LogLevel.Information;

    // The ELITE_LOG_LEVEL/SCR_LOG_LEVEL environment variables still override this for when the config file itself needs debugging.
    public LogLevel MinimumLevel { get; set; } = DefaultMinimumLevel;

    // How many rolling daily log files are kept before the oldest are
    // deleted.
    public int RetainedFileCount { get; set; } = DefaultRetainedFileCount;

    /// <summary>
    /// Replaces any logging value that cannot be honoured with its default,
    /// in place.
    /// </summary>
    /// <returns><see langword="true"/> if anything had to be replaced.</returns>
    public bool Repair()
    {
        bool repaired = false;

        if (!Enum.IsDefined(MinimumLevel))
        {
            MinimumLevel = DefaultMinimumLevel;
            repaired = true;
        }

        if (RetainedFileCount is < 1 or > MaxRetainedFileCount)
        {
            RetainedFileCount = DefaultRetainedFileCount;
            repaired = true;
        }

        return repaired;
    }
}

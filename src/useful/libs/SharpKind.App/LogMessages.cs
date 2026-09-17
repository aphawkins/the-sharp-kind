// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Microsoft.Extensions.Logging;

namespace SharpKind.App;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Starting {title}")]
    internal static partial void StartingTitle(ILogger logger, string title);

    [LoggerMessage(EventId = 1, Level = LogLevel.Critical, Message = "Application terminated unexpectedly")]
    internal static partial void CriticalAppTerminated(ILogger logger, Exception ex);

    // ":l" is Serilog's literal-rendering format specifier - without it the sink would wrap the whole JSON blob in an extra pair of quotes.
    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "{SystemInfoJson:l}")]
    internal static partial void SystemInfo(ILogger logger, string systemInfoJson);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "{EngineSettingsJson:l}")]
    internal static partial void EngineSettings(ILogger logger, string engineSettingsJson);
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using Microsoft.Extensions.Logging;

namespace BubbleBobbleSharpLib;

internal static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Skipped rendition '{Path}': it could not be read.")]
    internal static partial void RenditionAssemblyUnreadable(ILogger logger, string path, Exception ex);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Loaded {RenditionCount} rendition(s) from {AssemblyCount} plugin assemblies; drawing {Name}.")]
    internal static partial void RenditionsLoaded(ILogger logger, int renditionCount, int assemblyCount, string name);
}

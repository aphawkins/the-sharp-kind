// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.GoldenFrames;

namespace EliteSharpLib.Tests.GoldenTrace;

// Where Elite's committed baselines live. The mechanics are shared with
// Stunt Car Racer's - see BaselineFolder - and only the names are Elite's.
internal static class TraceBaselines
{
    internal const string RegenerateEnvVar = "ELITE_REGENERATE_TRACES";

    private static readonly BaselineFolder s_folder =
        new(RegenerateEnvVar, "EliteSharpLib.Tests.csproj", "GoldenTrace");

    internal static bool Regenerating => s_folder.Regenerating;

    internal static string OutputPath(string scenarioName, string extension = TraceFile.Extension)
        => s_folder.OutputPath(scenarioName, extension);

    internal static string SourcePath(string scenarioName, string extension = TraceFile.Extension)
        => s_folder.SourcePath(scenarioName, extension);
}

// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.GoldenFrames;

namespace StuntCarRacerSharpLib.Tests.GoldenFrames;

// Where SCR's committed baselines live. The mechanics are shared with
// Elite's - see BaselineFolder - and only the names are SCR's. Its own
// variable, so regenerating one game's baselines never rewrites the other's.
internal static class FrameBaselines
{
    internal const string RegenerateEnvVar = "SCR_REGENERATE_TRACES";

    private static readonly BaselineFolder s_folder =
        new(RegenerateEnvVar, "StuntCarRacerSharpLib.Tests.csproj", "GoldenFrames");

    internal static bool Regenerating => s_folder.Regenerating;

    internal static string OutputPath(string scenarioName)
        => s_folder.OutputPath(scenarioName, FrameFile.Extension);

    internal static string SourcePath(string scenarioName)
        => s_folder.SourcePath(scenarioName, FrameFile.Extension);
}

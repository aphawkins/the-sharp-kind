// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.GoldenFrames;

namespace BubbleBobbleSharpLib.Tests.GoldenFrames;

// The mechanics are shared with Elite's and Stunt Car Racer's - see BaselineFolder. Its own variable, so regenerating one game's
// baselines never rewrites another's.
internal static class FrameBaselines
{
    internal const string RegenerateEnvVar = "BB_REGENERATE_FRAMES";

    private static readonly BaselineFolder s_folder =
        new(RegenerateEnvVar, "BubbleBobbleSharpLib.Tests.csproj", "GoldenFrames");

    internal static bool Regenerating => s_folder.Regenerating;

    internal static string OutputPath(string scenarioName)
        => s_folder.OutputPath(scenarioName, FrameFile.Extension);

    internal static string SourcePath(string scenarioName)
        => s_folder.SourcePath(scenarioName, FrameFile.Extension);
}

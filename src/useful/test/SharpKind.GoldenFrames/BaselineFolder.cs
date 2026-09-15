// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.GoldenFrames;

/// <summary>
/// Where one test project's committed baselines live, and whether this run
/// is asserting against them or rewriting them.
/// </summary>
/// <remarks>
/// <para>
/// Reading goes through the build output, which the csproj copies the
/// baselines into, so a test run never depends on the source tree being
/// where it was compiled. Regenerating writes back to the source tree
/// instead, because the point of regenerating is to produce a diff to review
/// and commit - a rewritten copy under bin/ would be thrown away by the next
/// clean.
/// </para>
/// <para>
/// Each game names its own environment variable and its own project, so one
/// game's baselines can be regenerated without disturbing the other's.
/// </para>
/// </remarks>
/// <param name="RegenerateEnvVar">
/// The variable that switches this run from asserting to rewriting.
/// Deliberately not a flag on the test: regenerating is how a genuine
/// behaviour change is accepted, and it should take a conscious act outside
/// the test run.
/// </param>
/// <param name="ProjectFileName">
/// The test project's own csproj, used to find the source tree by walking up
/// from the build output. Not <c>[CallerFilePath]</c>: this repo builds with
/// deterministic source paths, so a caller path is the rewritten
/// <c>/_/src/...</c> and regenerating through it silently wrote the
/// baselines to <c>C:\_</c> instead of the working tree.
/// </param>
/// <param name="ProjectFolder">The folder inside the project holding them.</param>
public sealed record BaselineFolder(string RegenerateEnvVar, string ProjectFileName, string ProjectFolder)
{
    private const string Folder = "Baselines";

    public bool Regenerating
        => Environment.GetEnvironmentVariable(RegenerateEnvVar) is "1" or "true";

    public string OutputPath(string scenarioName, string extension)
        => Path.Combine(AppContext.BaseDirectory, ProjectFolder, Folder, scenarioName + extension);

    public string SourcePath(string scenarioName, string extension)
        => Path.Combine(SourceFolder(), Folder, scenarioName + extension);

    private string SourceFolder()
    {
        DirectoryInfo? folder = new(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, ProjectFileName)))
        {
            folder = folder.Parent;
        }

        return folder is null
            ? throw new InvalidOperationException(
                $"Cannot locate {ProjectFileName} above {AppContext.BaseDirectory}; regenerate from a source build.")
            : Path.Combine(folder.FullName, ProjectFolder);
    }
}

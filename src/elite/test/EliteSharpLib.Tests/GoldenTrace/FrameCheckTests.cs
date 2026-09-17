// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.GoldenFrames;

namespace EliteSharpLib.Tests.GoldenTrace;

/// <summary>
/// Checks what Elite actually draws, at a handful of ticks per scenario,
/// against committed signatures.
/// </summary>
/// <remarks>
/// <para>
/// The golden traces compare state and cannot see the order things are
/// drawn in. That matters for the rest of the simulate/compose split:
/// the starfield is drawn before the universe today, and moving it into a
/// compose pass without preserving that would paint the stars over the
/// ships with every trace still green. This is the check that would fail.
/// </para>
/// <para>
/// A frame is signed twice over - a 32x32 brightness grid, which is what is
/// compared and what a reviewer reads, and a hash of every pixel, which
/// names the frame. The grid is compared a cell at a time with a one-step
/// tolerance, because a pixel-exact hash is pinned to the machine that
/// regenerated it and fails elsewhere over rounding alone.
/// </para>
/// <para>
/// Regenerate with ELITE_REGENERATE_TRACES=1, the same switch the traces
/// use, and review the grid diff before committing.
/// </para>
/// </remarks>
[Trait("Level", "Integration")]

// Serialised against the other classes that read or write ELITE_DEBUG_YAW.
// EnvironmentVariableScope puts a variable back, but it cannot stop another
// class reading it in the meantime: an environment variable belongs to the
// process, and xUnit runs test classes in parallel.
[Collection("EnvironmentVariables")]
public class FrameCheckTests
{
    public static TheoryData<string> ScenarioNames
    {
        get
        {
            TheoryData<string> names = [];
            foreach (TraceScenario scenario in TraceScenarios.All)
            {
                names.Add(scenario.Name);
            }

            return names;
        }
    }

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void ScenarioFramesMatchTheirSignatures(string scenarioName)
    {
        TraceScenario scenario = TraceScenarios.All.Single(s => s.Name == scenarioName);
        Assert.NotEmpty(scenario.FrameTicks);

        string text = FrameFile.Write(scenario.Name, FrameRecorder.Record(scenario));

        if (TraceBaselines.Regenerating)
        {
            string path = TraceBaselines.SourcePath(scenario.Name, FrameFile.Extension);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
            Assert.Fail(
                $"Regenerated frames for '{scenario.Name}'. Review the diff, commit it, "
                    + $"then clear {TraceBaselines.RegenerateEnvVar}.");
        }

        string baselinePath = TraceBaselines.OutputPath(scenario.Name, FrameFile.Extension);
        Assert.True(
            File.Exists(baselinePath),
            $"No frame baseline for '{scenario.Name}'. Set {TraceBaselines.RegenerateEnvVar}=1 to create one.");

        IReadOnlyList<FrameSignature> expected = FrameFile.Read(File.ReadAllText(baselinePath));
        IReadOnlyList<FrameSignature> actual = FrameFile.Read(text);
        string? difference = FrameComparer.FindFirstDifference(expected, actual);

        Assert.True(difference is null, difference);
    }
}

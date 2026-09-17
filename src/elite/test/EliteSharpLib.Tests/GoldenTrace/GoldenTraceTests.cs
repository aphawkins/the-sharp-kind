// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

namespace EliteSharpLib.Tests.GoldenTrace;

/// <summary>
/// Characterization tests for Elite's fixed 13.5Hz simulation: three
/// scripted runs, recorded tick by tick against committed baselines.
/// </summary>
/// <remarks>
/// <para>
/// These assert nothing about whether the game is <i>right</i> - only that
/// it still does what it did. They are the regression net for the
/// frame-rate rework in backlog-roadmap.md, which converts every per-tick
/// rate and counter in the game to a per-second one. Undoing the
/// simulate/compose fusion (item 2) must leave these bit-identical;
/// converting the rates (items 3 onward) must leave them within a tolerance
/// that session states and justifies.
/// </para>
/// <para>
/// To accept a genuine behaviour change, set ELITE_REGENERATE_TRACES=1, run
/// these tests, and review the resulting diff before committing it. A
/// baseline that changes without a reason in the commit message is the
/// thing this test exists to prevent.
/// </para>
/// </remarks>
[Trait("Level", "Integration")]

// Serialised against other classes reading/writing ELITE_DEBUG_YAW: it belongs to the process, and xUnit runs test classes in parallel.
[Collection("EnvironmentVariables")]
public class GoldenTraceTests
{
    // Zero until the rate conversion starts. Items 1 and 2 change no
    // arithmetic, so anything above zero here would only hide a mistake.
    private const float Tolerance = 0f;

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
    public void ScenarioMatchesItsBaseline(string scenarioName)
    {
        TraceScenario scenario = TraceScenarios.All.Single(s => s.Name == scenarioName);
        string text = TraceFile.Write(scenario, TraceRecorder.Record(scenario));

        if (TraceBaselines.Regenerating)
        {
            string path = TraceBaselines.SourcePath(scenario.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
            Assert.Fail(
                $"Regenerated '{scenario.Name}'. Review the diff, commit it, "
                    + $"then clear {TraceBaselines.RegenerateEnvVar}.");
        }

        // Compared as stored, not recorded: the file's four decimals are finer than a float's own
        // resolution, so putting both sides through it makes zero tolerance mean the strongest claim the format supports.
        IReadOnlyList<TraceSample> actual = TraceFile.Read(text);

        string baselinePath = TraceBaselines.OutputPath(scenario.Name);
        Assert.True(
            File.Exists(baselinePath),
            $"No baseline for '{scenario.Name}'. Set {TraceBaselines.RegenerateEnvVar}=1 and run again to create one.");

        IReadOnlyList<TraceSample> expected = TraceFile.Read(File.ReadAllText(baselinePath));
        string? difference = TraceComparer.FindFirstDifference(expected, actual, Tolerance);

        Assert.True(
            difference is null,
            $"'{scenario.Name}' diverged from its baseline at {difference}");
    }

    // The recording has to be reproducible before it is worth committing:
    // a trace that differs run to run would fail for the wrong reason and
    // get regenerated until it happened to pass.
    //
    // It now proves something stronger than that. Only the game's RNG is
    // seeded; RenderRandom - the laser shimmer and the explosion scatter -
    // is left unseeded on purpose. So if drawing could ever influence the
    // simulation again, as it did before the two streams were separated,
    // this test would start failing at random rather than the coupling
    // going unnoticed.
    [Fact]
    public void RecordingTheSameScenarioTwiceGivesTheSameTrace()
    {
        TraceScenario scenario = TraceScenarios.IntroParade;

        string first = TraceFile.Write(scenario, TraceRecorder.Record(scenario));
        string second = TraceFile.Write(scenario, TraceRecorder.Record(scenario));

        Assert.Equal(first, second);
    }

    // A round-trip that drifted would let a baseline record one thing and compare as another.
    [Fact]
    public void WritingAndReadingATraceRoundTrips()
    {
        TraceScenario scenario = TraceScenarios.LaunchAndFly;

        string text = TraceFile.Write(scenario, TraceRecorder.Record(scenario));
        IReadOnlyList<TraceSample> reread = TraceFile.Read(text);

        Assert.Equal(text, TraceFile.Write(scenario, reread));
        Assert.Null(TraceComparer.FindFirstDifference(reread, TraceFile.Read(text), Tolerance));
    }
}

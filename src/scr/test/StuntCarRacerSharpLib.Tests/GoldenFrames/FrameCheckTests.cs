// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.GoldenFrames;
using Xunit;

namespace StuntCarRacerSharpLib.Tests.GoldenFrames;

/// <summary>
/// Checks what Stunt Car Racer actually draws, at a handful of ticks per
/// screen, against committed signatures.
/// </summary>
/// <remarks>
/// <para>
/// SCR had nothing of this kind, so a rendering change to it was unguarded
/// where Elite's was checked byte for byte. It matters most for the layer
/// work: moving the backdrop and the world polygons into layers of their own
/// is precisely the sort of change that keeps every behavioural test green
/// while painting the world over the cockpit, or the backdrop over both.
/// </para>
/// <para>
/// A frame is signed twice over - an exact hash of every pixel, which
/// catches any change at all, and a 32x32 brightness grid, which shows
/// where the change is. The grid is the half a reviewer reads.
/// </para>
/// <para>
/// Regenerate with SCR_REGENERATE_TRACES=1 and review the grid diff before
/// committing.
/// </para>
/// </remarks>
[Trait("Level", "Integration")]
public class FrameCheckTests
{
    public static TheoryData<string> ScenarioNames
    {
        get
        {
            TheoryData<string> names = [];
            foreach (FrameScenario scenario in FrameScenarios.All)
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
        FrameScenario scenario = FrameScenarios.All.Single(s => s.Name == scenarioName);
        Assert.NotEmpty(scenario.FrameTicks);

        string text = FrameFile.Write(scenario.Name, FrameRecorder.Record(scenario));

        if (FrameBaselines.Regenerating)
        {
            string path = FrameBaselines.SourcePath(scenario.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
            Assert.Fail(
                $"Regenerated frames for '{scenario.Name}'. Review the diff, commit it, "
                    + $"then clear {FrameBaselines.RegenerateEnvVar}.");
        }

        string baselinePath = FrameBaselines.OutputPath(scenario.Name);
        Assert.True(
            File.Exists(baselinePath),
            $"No frame baseline for '{scenario.Name}'. Set {FrameBaselines.RegenerateEnvVar}=1 to create one.");

        IReadOnlyList<FrameSignature> expected = FrameFile.Read(File.ReadAllText(baselinePath));
        IReadOnlyList<FrameSignature> actual = FrameFile.Read(text);

        // Assert.True rather than Assert.Null: the difference string carries
        // the two thumbnail grids side by side, and Assert.Null truncates it
        // to the first few characters - which throws away the half a reviewer
        // actually reads.
        string? difference = FrameComparer.FindFirstDifference(expected, actual);
        Assert.True(difference is null, difference);
    }

    // The baselines are only worth committing if the same script composes the
    // same pixels every run. It does not in the shipped game - the opponent's
    // steering and the engine note are drawn from Random.Shared - so this
    // checks the harness's seed actually pins them.
    [Fact]
    public void TheSameScenarioComposesTheSameFrameTwice()
    {
        FrameScenario scenario = FrameScenarios.Race;

        IReadOnlyList<FrameSignature> first = FrameRecorder.Record(scenario);
        IReadOnlyList<FrameSignature> second = FrameRecorder.Record(scenario);

        Assert.NotEmpty(first);
        string? difference = FrameComparer.FindFirstDifference(first, second);
        Assert.True(difference is null, difference);
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.GoldenFrames;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.GoldenFrames;

/// <summary>
/// Checks what Bubble Bobble actually draws, at three ticks of each of its hundred levels, against committed signatures.
/// </summary>
/// <remarks>
/// <para>
/// The other tests prove the game's routines and its views one at a time. This is the check that puts them together: the
/// level, its items, the bubbles and the sprites, in the order they are composed, on the shipped artwork, which no
/// single view's test can see. A change that moves a layer or repaints a sheet wrongly fails here on every level it touches.
/// </para>
/// <para>
/// A frame is signed twice over - a 32x32 brightness grid, which is what is compared and what a reviewer reads, and a hash of
/// every pixel, which names the frame. The grid is compared a cell at a time with a one-step tolerance, because a pixel-exact
/// hash is pinned to the machine that regenerated it and fails elsewhere over rounding alone.
/// </para>
/// <para>
/// Regenerate with BB_REGENERATE_FRAMES=1 and review the grid diff before committing.
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
    public void LevelFramesMatchTheirSignatures(string scenarioName)
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

        // Assert.True, not Assert.Null: Assert.Null truncates the diff string, throwing away the grids a reviewer reads.
        string? difference = FrameComparer.FindFirstDifference(expected, actual);
        Assert.True(difference is null, difference);
    }

    // The game rolls from a shared Random unless it is given one: checks the seed pins every frame.
    [Fact]
    public void TheSameScenarioComposesTheSameFramesTwice()
    {
        FrameScenario scenario = FrameScenarios.Level(1);

        IReadOnlyList<FrameSignature> first = FrameRecorder.Record(scenario);
        IReadOnlyList<FrameSignature> second = FrameRecorder.Record(scenario);

        Assert.NotEmpty(first);
        string? difference = FrameComparer.FindFirstDifference(first, second);
        Assert.True(difference is null, difference);
    }
}

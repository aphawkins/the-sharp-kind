// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.GoldenFrames;

namespace BubbleBobbleSharpLib.Tests.GoldenFrames;

// Runs a scenario and signs the composed frame at each of its FrameTicks.
internal static class FrameRecorder
{
    internal static IReadOnlyList<FrameSignature> Record(FrameScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        using BbHeadlessHarness harness = new(scenario.Level, scenario.RandomSeed);

        List<FrameSignature> frames = [];
        int last = scenario.FrameTicks.Count == 0 ? -1 : scenario.FrameTicks.Max();

        for (int tick = 0; tick <= last; tick++)
        {
            _ = harness.Step(scenario.Script);

            if (scenario.FrameTicks.Contains(tick))
            {
                frames.Add(FrameSignature.Capture(tick, harness.CaptureFrame()));
            }
        }

        return frames;
    }
}

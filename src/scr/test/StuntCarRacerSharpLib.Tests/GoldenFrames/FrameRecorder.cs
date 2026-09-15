// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.GoldenFrames;

namespace StuntCarRacerSharpLib.Tests.GoldenFrames;

// Runs a scenario and signs the composed frame at each of its FrameTicks.
internal static class FrameRecorder
{
    internal static IReadOnlyList<FrameSignature> Record(FrameScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        using HeadlessGameHarness harness = new(randomSeed: scenario.RandomSeed);

        List<FrameSignature> frames = [];
        int last = scenario.FrameTicks.Count == 0 ? -1 : scenario.FrameTicks.Max();

        for (int tick = 0; tick <= last; tick++)
        {
            harness.Step(scenario.Script);

            if (scenario.FrameTicks.Contains(tick))
            {
                frames.Add(FrameSignature.Capture(tick, harness.CaptureFrame()));
            }
        }

        return frames;
    }
}

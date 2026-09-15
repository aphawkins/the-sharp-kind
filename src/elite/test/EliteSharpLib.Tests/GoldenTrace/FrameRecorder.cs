// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Save;
using SharpKind.GoldenFrames;

namespace EliteSharpLib.Tests.GoldenTrace;

// Runs a scenario and signs the composed frame at each of its FrameTicks.
//
// Unlike TraceRecorder this calls Draw() as well as Update(), because the
// frame is the point. Elite composes inside Update today and Draw only
// presents, so the capture is of whatever the tick just built.
internal static class FrameRecorder
{
    internal static IReadOnlyList<FrameSignature> Record(TraceScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        // Same reason as TraceRecorder: a frame must show the commander the
        // game ships with, not the one the machine asks for. Commander Max
        // carries different equipment, and the HUD draws it.
        using EnvironmentVariableScope commander =
            EnvironmentVariableScope.Set(SaveFile.DebugCommanderEnvVar, null);
        using EnvironmentVariableScope yaw = EnvironmentVariableScope.Set(DebugYaw.EnvVar, null);

        // Unlike the traces, this one pins the drawing's stream too: the
        // starfield is scattered from it, and a frame hash cannot be
        // compared against a committed one if the stars move each run.
        using HeadlessGameHarness harness = new(
            randomSeed: scenario.RandomSeed,
            renderSeed: scenario.RandomSeed);

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

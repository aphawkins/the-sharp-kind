// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Input;

namespace EliteSharpLib.Tests.GoldenTrace;

// A fixed seed, key script and length, so the same scenario always produces the same trace.
//
// FrameTicks names ticks whose composed frame is also checked - deliberately a handful, since
// traces already cover the game's state and frames only need to cover draw order.
internal sealed record TraceScenario(
    string Name,
    int RandomSeed,
    int Ticks,
    IReadOnlyList<KeyScriptEvent> Script,
    IReadOnlyList<int> FrameTicks);

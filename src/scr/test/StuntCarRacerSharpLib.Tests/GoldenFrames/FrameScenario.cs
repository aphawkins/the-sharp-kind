// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.Input;

namespace StuntCarRacerSharpLib.Tests.GoldenFrames;

// One recorded run: a fixed seed, a fixed key script and the ticks whose
// composed frame is checked against a committed signature.
//
// Elite pairs its frames with state traces and keeps the frames sparse
// because the traces already cover what the game is doing. SCR has no
// traces, so here the frames are the whole check and the tick list is what
// decides how much of the game is covered.
internal sealed record FrameScenario(
    string Name,
    int RandomSeed,
    IReadOnlyList<KeyScriptEvent> Script,
    IReadOnlyList<int> FrameTicks);

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Input;

namespace BubbleBobbleSharpLib.Tests.GoldenFrames;

// One recorded run: a level, a fixed seed, a fixed key script and the ticks (updates of the game, 25 a second) whose composed
// frame is checked against a committed signature.
internal sealed record FrameScenario(
    string Name,
    int Level,
    int RandomSeed,
    IReadOnlyList<KeyScriptEvent> Script,
    IReadOnlyList<int> FrameTicks);

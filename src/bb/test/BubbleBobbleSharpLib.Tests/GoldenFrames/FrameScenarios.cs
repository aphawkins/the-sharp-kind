// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using SharpKind.Input;

namespace BubbleBobbleSharpLib.Tests.GoldenFrames;

// Every level of the game, played by a stick that walks and jumps, at three moments: as the level opens (the enemies dropping
// in, READY!!'s time on the clock), a few seconds in, and once the first bubbles and enemies have been at each other.
internal static class FrameScenarios
{
    private const int Seed = 1986;

    private static readonly int[] s_ticks = [2, 60, 200];

    // Right for two seconds with a jump every second, then the fire key held to blow bubbles, then left.
    private static readonly IReadOnlyList<KeyScriptEvent> s_script = KeyScriptParser.Parse(
        """
        0 hold RightArrow
        20 tap Spacebar
        45 tap Spacebar
        50 release RightArrow
        60 hold Spacebar
        90 release Spacebar
        100 hold LeftArrow
        120 tap Spacebar
        150 release LeftArrow
        """);

    internal static IReadOnlyList<FrameScenario> All { get; } = [.. Enumerable.Range(1, 100).Select(Level)];

    internal static FrameScenario Level(int number) => new(
        string.Create(CultureInfo.InvariantCulture, $"level-{number:D3}"),
        number,
        Seed,
        s_script,
        s_ticks);
}

// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Views;
using SharpKind.Input;

namespace EliteSharpLib.Tests;

[Trait("Level", "Integration")]
public class HeadlessGameHarnessTests
{
    [Fact]
    public void InitialStateIsNoScreenBeforeFirstUpdate()
    {
        using HeadlessGameHarness harness = new();

        GameStateSummary state = harness.State;

        Assert.Equal(Screen.None, state.Screen);
    }

    [Fact]
    public void ScriptedKeysDriveIntroThroughToCommanderStatus()
    {
        using HeadlessGameHarness harness = new();

        // Tick 0 runs InitialiseGame(), clearing pressed keys, so the script's first key lands on
        // tick 1. N answers "Load New Commander?" No (IntroOne -> IntroTwo); Space at tick 2 leaves the parade.
        KeyScriptEvent[] script =
        [
            new(1, ConsoleKey.N, KeyScriptAction.Tap),
            new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap),
        ];

        GameStateSummary state = harness.Run(3, script);

        Assert.Equal(Screen.CommanderStatus, state.Screen);
        Assert.True(state.IsDocked);
        Assert.False(state.IsGameOver);
    }

    // Intro screens own the title music and stop it on the way out; a chart/options key that skipped this left it playing behind the game.
    [Theory]
    [InlineData(ConsoleKey.F5)]
    [InlineData(ConsoleKey.F9)]
    [InlineData(ConsoleKey.F11)]
    public void ViewKeysDoNotLeaveTheIntro(ConsoleKey key)
    {
        using HeadlessGameHarness harness = new();

        // N at tick 2 proves the intro still has the input, not just that it's sitting on a screen that ignores it.
        KeyScriptEvent[] script =
        [
            new(1, key, KeyScriptAction.Tap),
            new(2, ConsoleKey.N, KeyScriptAction.Tap),
        ];

        Assert.Equal(Screen.IntroOne, harness.Run(2, script).Screen);
        Assert.Equal(Screen.IntroTwo, harness.Run(1, script).Screen);
    }

    [Fact]
    public void SaveFrameWritesABmpOfTheWholeGame()
    {
        using HeadlessGameHarness harness = new();
        harness.Run(2, []);

        string path = Path.Combine(Path.GetTempPath(), $"elite_harness_frame_{Guid.NewGuid():N}.bmp");
        try
        {
            harness.SaveFrame(path);

            FileInfo info = new(path);
            Assert.True(info.Exists);
            Assert.True(info.Length > 0);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

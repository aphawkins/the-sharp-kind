// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.Input;
using StuntCarRacerSharpLib.Screens;
using Xunit;

namespace StuntCarRacerSharpLib.Tests;

[Trait("Level", "Integration")]
public class HeadlessGameHarnessTests
{
    [Fact]
    public void InitialStateIsTrackMenuWithNoRaceStarted()
    {
        using HeadlessGameHarness harness = new();

        GameStateSummary state = harness.State;

        Assert.Equal(GameMode.TrackMenu, state.Screen);
        Assert.False(state.RaceStarted);
    }

    [Fact]
    public void ScriptedKeysDriveMenuThroughToRace()
    {
        using HeadlessGameHarness harness = new();

        // S at tick 0 selects the track: menu -> preview. Held to tick 4 to give the preview's physics gate a chance to read it.
        KeyScriptEvent[] script =
        [
            new(0, ConsoleKey.S, KeyScriptAction.Tap),
            new(1, ConsoleKey.S, KeyScriptAction.Hold),
            new(5, ConsoleKey.S, KeyScriptAction.Release),
        ];

        GameStateSummary state = harness.Run(6, script);

        Assert.Equal(GameMode.GameInProgress, state.Screen);
        Assert.True(state.RaceStarted);
    }

    [Fact]
    public void StateTracksOpponentProgressDuringARace()
    {
        using HeadlessGameHarness harness = new();
        KeyScriptEvent[] startRace =
        [
            new(0, ConsoleKey.S, KeyScriptAction.Tap),
            new(1, ConsoleKey.S, KeyScriptAction.Hold),
            new(5, ConsoleKey.S, KeyScriptAction.Release),
        ];
        harness.Run(6, startRace);
        int opponentPieceAtRaceStart = harness.State.OpponentPiece;

        // no scripted input for the player: the opponent still drives
        // itself, so its track piece should move on regardless.
        GameStateSummary state = harness.Run(200, []);

        Assert.Equal(GameMode.GameInProgress, state.Screen);
        Assert.NotEqual(opponentPieceAtRaceStart, state.OpponentPiece);
    }

    [Fact]
    public void SaveFrameWritesABmpOfTheWholeGame()
    {
        using HeadlessGameHarness harness = new();
        harness.Run(2, []);

        string path = Path.Combine(Path.GetTempPath(), $"harness_frame_{Guid.NewGuid():N}.bmp");
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

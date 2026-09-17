// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind.Input;

namespace StuntCarRacerSharpLib.Tests.GoldenFrames;

// The scenarios the baselines cover: the three screens the game draws, one
// each. Between them they reach every layer a frame has - the backdrop and
// the world polygons that the layer work will move, and the text and cockpit
// drawn over them.
//
// S advances the screen: menu -> preview -> race, one tap each, the same
// sequence StuntCarRacerMainTests.StartRace uses.
internal static class FrameScenarios
{
    // Only that it never changes.
    private const int Seed = 20260915;

    // The one screen that would still look right if the world layers drew nothing, hence its own baseline rather than being assumed covered by the race.
    internal static FrameScenario TrackMenu { get; } = new(
        "track-menu",
        Seed,
        [],
        [2, 10]);

    // The screen that draws world polygons with no cockpit over them, so a backdrop painted over the world would show most plainly.
    internal static FrameScenario TrackPreview { get; } = new(
        "track-preview",
        Seed,
        [
            new(1, ConsoleKey.S, KeyScriptAction.Tap),
        ],

        // Early, then far enough in for the preview's own rotation to have
        // moved the circuit a long way round.
        [5, 60, 140]);

    // Accelerator held, not tapped, so the frames show a car actually moving; a stationary car would leave the physics out of the check.
    internal static FrameScenario Race { get; } = new(
        "race",
        Seed,
        [
            new(1, ConsoleKey.S, KeyScriptAction.Tap),
            new(3, ConsoleKey.S, KeyScriptAction.Tap),
            new(10, ConsoleKey.A, KeyScriptAction.Hold),
        ],

        // Just after the lights, then well into the first stretch, then
        // later still with the opponent's own run under way.
        [12, 80, 200]);

    internal static IReadOnlyList<FrameScenario> All { get; } = [TrackMenu, TrackPreview, Race];
}

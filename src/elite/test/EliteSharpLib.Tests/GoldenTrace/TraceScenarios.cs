// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Input;

namespace EliteSharpLib.Tests.GoldenTrace;

// The scenarios the baselines cover. Between them they exercise every rate
// and counter the seven frame-rate items in backlog-roadmap.md touch; the
// comment on each says which, so a scenario is not dropped later without
// noticing what it was holding down.
//
// Tick 0 runs InitialiseGame(), which sets the view to IntroOne and clears
// any pressed keys on the way in - so a script's first key can only land on
// tick 1.
internal static class TraceScenarios
{
    // Elite's own choice of seed matters only in that it never changes.
    private const int Seed = 20260826;

    // The title screen and ship parade. Covers Intro1/Intro2Controller's per-tick Z step and the
    // parade ship's rotation via Space.SpinUniverseObject and RotateXFirst.
    internal static TraceScenario IntroParade { get; } = new(
        "intro-parade",
        Seed,
        260,
        [
            new(1, ConsoleKey.N, KeyScriptAction.Tap),
        ],

        // A parade ship close and far, both against the starfield: the two
        // things whose drawing order matters most on this screen.
        [20, 150]);

    // Docked, launched, then flown. Covers BreakPattern's 20 rings, Space.LaunchPlayer, universe
    // motion under MoveUniverseObject/ApplyShipVelocity, and the roll/pitch ramp PilotController steps twice a tick.
    internal static TraceScenario LaunchAndFly { get; } = new(
        "launch-and-fly",
        Seed,
        200,
        [
            new(1, ConsoleKey.N, KeyScriptAction.Tap),
            new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap),

            // F1 while docked is Undocking, which is LaunchView.
            new(4, ConsoleKey.F1, KeyScriptAction.Tap),

            // Accelerate for a while, then roll and pitch into it.
            new(40, ConsoleKey.Spacebar, KeyScriptAction.Hold),
            new(90, ConsoleKey.Spacebar, KeyScriptAction.Release),
            new(90, ConsoleKey.OemPeriod, KeyScriptAction.Hold),
            new(130, ConsoleKey.OemPeriod, KeyScriptAction.Release),
            new(130, ConsoleKey.S, KeyScriptAction.Hold),
            new(170, ConsoleKey.S, KeyScriptAction.Release),
        ],

        // Mid break-pattern, then the front view with the station and the
        // planet in it, then again mid-roll - the frame at its busiest.
        [10, 60, 150]);

    // The same launch, left alone long enough for MCount (255 down to 0) to reach shield regen,
    // energy-low/altitude/cabin-temp checks and a random encounter, plus Combat's tactics pacing.
    internal static TraceScenario LongFlight { get; } = new(
        "long-flight",
        Seed,
        600,
        [
            new(1, ConsoleKey.N, KeyScriptAction.Tap),
            new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap),
            new(4, ConsoleKey.F1, KeyScriptAction.Tap),
            new(40, ConsoleKey.Spacebar, KeyScriptAction.Hold),
            new(120, ConsoleKey.Spacebar, KeyScriptAction.Release),
        ],
        [80, 300]);

    // Launched, then the trigger held down. Covers what the flying scenarios
    // never touch: Combat.FireLaser, the laser temperature climbing and
    // CoolLaser's per-tick -2, and PilotController's own _drawLaserFrames
    // countdown.
    internal static TraceScenario LaserFire { get; } = new(
        "laser-fire",
        Seed,
        160,
        [
            new(1, ConsoleKey.N, KeyScriptAction.Tap),
            new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap),
            new(4, ConsoleKey.F1, KeyScriptAction.Tap),

            // Held well past the point the laser overheats and cuts out, so
            // the cooling ramp is in the trace as well as the heating one.
            new(30, ConsoleKey.A, KeyScriptAction.Hold),
            new(110, ConsoleKey.A, KeyScriptAction.Release),
        ],

        // While the beam is drawn, and after it has stopped.
        [40, 120]);

    // Launched, turned onto the encounter behind, then shot - the one scenario reaching the
    // explosion cloud (DrawObject seeds ExpDelta at 18, DrawExplosion advances it by four per frame past 251, then Remove).
    //
    // At this seed a Shuttle arrives behind and stays there, so the script pitches 230 ticks to
    // bring it dead ahead and holds the beam. It dies at tick 376, cloud draws 377-435. The wreck
    // must be in front for DrawExplosion (which returns on Location.Z <= 0) to see it - a ship
    // flying into a held beam is already past by the time it dies, which a fire-and-wait script couldn't aim for.
    //
    // The turn duration is a property of the seed: a change to how much RNG the game draws moves
    // the trigger tick. That's a real divergence, but a trace with no explosion means the scenario
    // has stopped covering what it's for and should be re-aimed, not left green.
    //
    // Nothing scripts a kill more directly: the escape capsule and energy bomb are the only other
    // deterministic explosions, and both belong to Commander Max, whom TraceRecorder turns off.
    internal static TraceScenario Explosion { get; } = new(
        "explosion",
        Seed,
        480,
        [
            new(1, ConsoleKey.N, KeyScriptAction.Tap),
            new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap),
            new(4, ConsoleKey.F1, KeyScriptAction.Tap),

            // Pitch until the encounter behind is dead ahead.
            new(130, ConsoleKey.S, KeyScriptAction.Hold),
            new(360, ConsoleKey.S, KeyScriptAction.Release),

            // Never released: the beam has to still be firing when it dies.
            new(360, ConsoleKey.A, KeyScriptAction.Hold),
        ],

        // The cloud early and late, then just after the wreck is gone.
        [385, 425, 445]);

    internal static IReadOnlyList<TraceScenario> All { get; } =
        [IntroParade, LaunchAndFly, LongFlight, LaserFire, Explosion];
}

// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Ships;
using SharpKind.Input;

namespace EliteSharpLib.Tests;

/// <summary>
/// The four game runs <see cref="FrameRateIndependenceTests"/> compares,
/// played once each and shared by every test in the class.
/// </summary>
/// <remarks>
/// Each test used to start its own pair of runs, so seven tests played the
/// game fourteen times for eight distinct runs. Worse, the two throttle tests
/// stopped part way up a ramp the longer runs pass through anyway. A run
/// records the speed as it goes by, so the flight pair also serves the launch
/// throttle test and the docking pair serves both halves of the autopilot's,
/// leaving four runs in total.
/// </remarks>
// Public, and CA1515 waived: xunit's IClassFixture<T> requires a public test class, which can't name an internal type (CS0051).
#pragma warning disable CA1515
public sealed class FrameRateRuns : IDisposable
{
    private const float FastRate = 60f;

    // Long enough to launch, fly, and take a few hundred housekeeping steps.
    private const int SlowUpdates = 300;

    // The update the accelerate key goes down on, at the game's own rate.
    private const int LaunchUpdates = 40;

    // The update the docking computer is engaged on: far enough in that the
    // ship is at full speed and well clear of the station.
    private const int EngageUpdates = 80;

    // Sample points for the throttle ramp's two halves: slowing down, then speeding back up after the turn.
    private const int WindDownUpdates = 108;
    private const int WindUpUpdates = 175;

    // The update the player's own throttle is sampled at, part way up its
    // ramp rather than at the ceiling it saturates against.
    private const int ThrottleUpdates = LaunchUpdates + 13;

    private bool _isDisposed;

    public FrameRateRuns()
    {
        SlowFlight = Fly(GameClock.StepsPerSecond);
        FastFlight = Fly(FastRate);
        SlowDocking = Dock(GameClock.StepsPerSecond);
        FastDocking = Dock(FastRate);
    }

    // Launched and flown to SlowUpdates' worth of seconds, sampling the
    // player's throttle on the way past ThrottleUpdates.
    internal FrameRateRun SlowFlight { get; }

    internal FrameRateRun FastFlight { get; }

    // Launched, handed a docking computer and left to it, to WindUpUpdates'
    // worth of seconds, sampling the throttle at both WindDownUpdates and
    // WindUpUpdates.
    internal FrameRateRun SlowDocking { get; }

    internal FrameRateRun FastDocking { get; }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        SlowFlight.Dispose();
        FastFlight.Dispose();
        SlowDocking.Dispose();
        FastDocking.Dispose();
        _isDisposed = true;
    }

    // Runs the game at the given rate, with the key script stretched so each
    // press lands at the same moment in time rather than on the same update
    // number.
    private static FrameRateRun Fly(float updatesPerSecond)
    {
        float scale = updatesPerSecond / GameClock.StepsPerSecond;
        KeyScriptEvent[] script =
        [
            new((int)(1 * scale), ConsoleKey.N, KeyScriptAction.Tap),
            new((int)(2 * scale), ConsoleKey.Spacebar, KeyScriptAction.Tap),
            new((int)(4 * scale), ConsoleKey.F1, KeyScriptAction.Tap),
            new((int)(LaunchUpdates * scale), ConsoleKey.Spacebar, KeyScriptAction.Hold),
            new((int)(90 * scale), ConsoleKey.Spacebar, KeyScriptAction.Release),
        ];

        FrameRateRun run = new(updatesPerSecond);
        run.RunTo((int)(ThrottleUpdates * scale), script);
        run.SampleSpeed();
        run.RunTo((int)(SlowUpdates * scale), script);
        return run;
    }

    // Fitted mid-run, since a new commander ('N' at script start) has no docking computer.
    private static FrameRateRun Dock(float updatesPerSecond)
    {
        float scale = updatesPerSecond / GameClock.StepsPerSecond;
        int engage = (int)(EngageUpdates * scale);
        KeyScriptEvent[] script =
        [
            new((int)(1 * scale), ConsoleKey.N, KeyScriptAction.Tap),
            new((int)(2 * scale), ConsoleKey.Spacebar, KeyScriptAction.Tap),
            new((int)(4 * scale), ConsoleKey.F1, KeyScriptAction.Tap),
            new((int)(LaunchUpdates * scale), ConsoleKey.Spacebar, KeyScriptAction.Hold),
            new((int)(70 * scale), ConsoleKey.Spacebar, KeyScriptAction.Release),
            new(engage, ConsoleKey.C, KeyScriptAction.Tap),
        ];

        FrameRateRun run = new(updatesPerSecond);
        run.RunTo(engage, script);
        run.Harness.Resolve<PlayerShip>().HasDockingComputer = true;
        run.RunTo((int)(WindDownUpdates * scale), script);
        run.SampleSpeed();
        run.RunTo((int)(WindUpUpdates * scale), script);
        run.SampleSpeed();
        return run;
    }
}
#pragma warning restore CA1515

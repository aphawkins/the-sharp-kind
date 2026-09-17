// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Ships;
using SharpKind.Input;

namespace EliteSharpLib.Tests;

// The harness is kept afterwards so a test can also ask about the state the run finished in.
internal sealed class FrameRateRun : IDisposable
{
    private readonly List<float> _speeds = [];
    private bool _isDisposed;

    // The seed replaces the app's unseeded Random.Shared, so both rates roll
    // the same dice and a run can be repeated exactly.
    internal FrameRateRun(float updatesPerSecond)
        => Harness = new(randomSeed: 4242, updatesPerSecond: updatesPerSecond);

    internal HeadlessGameHarness Harness { get; }

    // The speeds recorded by SampleSpeed, in the order they were taken.
    internal IReadOnlyList<float> Speeds => _speeds;

    public void Dispose()
    {
        if (!_isDisposed)
        {
            Harness.Dispose();
            _isDisposed = true;
        }
    }

    // Advances to the given update, counted from the start of the run. A
    // target already passed does nothing.
    internal void RunTo(int update, IReadOnlyList<KeyScriptEvent> script)
        => Harness.Run(Math.Max(0, update - Harness.Tick), script);

    internal void SampleSpeed() => _speeds.Add(Harness.Resolve<PlayerShip>().Speed);
}

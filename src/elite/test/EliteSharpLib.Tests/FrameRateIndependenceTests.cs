// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Ships;
using EliteSharpLib.Views;

namespace EliteSharpLib.Tests;

/// <summary>
/// The same stretch of game played at two different update rates.
/// </summary>
/// <remarks>
/// This is what the whole frame-rate rework was for, and the only test that
/// puts the pieces together: the housekeeping clock, the motion, the
/// animations and the AI pacing all converted separately, each proved
/// unchanged at the game's own 13.5Hz. Here the game is run at 60Hz for the
/// same number of seconds and asked to be in the same place. The runs
/// themselves are in <see cref="FrameRateRuns"/>, played once for the class.
/// </remarks>
[Trait("Level", "Integration")]
public class FrameRateIndependenceTests(FrameRateRuns runs) : IClassFixture<FrameRateRuns>
{
    [Fact]
    public void TheHousekeepingTakesTheSameStepsInTheSameTime()
    {
        // Within one step: the clock keeps its unspent remainder, so different update counts over
        // the same elapsed time buy the same steps, apart from whichever truncation leaves half-finished.
        int slow = runs.SlowFlight.Harness.Game.State.MCount;
        int fast = runs.FastFlight.Harness.Game.State.MCount;

        Assert.InRange(fast, slow - 1, slow + 1);
    }

    [Fact]
    public void TheGameEndsUpOnTheSameScreenHavingLaunched()
    {
        Assert.Equal(Screen.FrontView, runs.SlowFlight.Harness.Game.State.CurrentScreen);
        Assert.Equal(Screen.FrontView, runs.FastFlight.Harness.Game.State.CurrentScreen);
        Assert.False(runs.SlowFlight.Harness.Game.State.IsDocked);
        Assert.False(runs.FastFlight.Harness.Game.State.IsDocked);
    }

    [Fact]
    public void TheShipHasTravelledTheSameDistance()
    {
        // Not to the bit: a first-order approximation diverges slightly between one long step and
        // four short ones. What matters is a fraction of a percent, not the fourfold factor this fixes.
        float slowDistance = DistanceToPlanet(runs.SlowFlight);
        float fastDistance = DistanceToPlanet(runs.FastFlight);

        Assert.True(slowDistance > 0, "the slow run never found a planet to measure against");
        Assert.Equal(slowDistance, fastDistance, slowDistance * 0.02f);
    }

    // Only true since the starfield stopped drawing from the game's random stream - it used to,
    // which broke this, since something else drawing from the stream per update shifts the dice.
    [Fact]
    public void BothRatesMeetTheSameShips()
        => Assert.Equal(ShipTypes(runs.SlowFlight), ShipTypes(runs.FastFlight));

    [Fact]
    public void TheThrottleRampsAtTheSameSpeed()
    {
        // Sampled part way up the ramp on purpose: a throttle winding up four times too fast is invisible at the end of a long flight.
        float slowSpeed = runs.SlowFlight.Speeds[0];

        Assert.InRange(slowSpeed, 13f, runs.SlowFlight.Harness.Resolve<PlayerShip>().MaxSpeed - 1);
        Assert.Equal(slowSpeed, runs.FastFlight.Speeds[0], 1.5f);
    }

    [Fact]
    public void TheDockingComputerWindsTheThrottleDownAtTheSameSpeed()
    {
        // The autopilot's own throttle, engaged pointing away at full speed so it winds down first.
        // Sampled part way: a saturated rest sample can't distinguish the correct rate from a fourfold one.
        float slowSpeed = runs.SlowDocking.Speeds[0];

        Assert.InRange(slowSpeed, 5f, 21f);
        Assert.Equal(slowSpeed, runs.FastDocking.Speeds[0], 1.5f);
    }

    [Fact]
    public void TheDockingComputerWindsTheThrottleUpAtTheSameSpeed()
    {
        // The other half: once turned around, the ship accelerates towards the station, caught mid-ramp to cover that branch.
        float slowSpeed = runs.SlowDocking.Speeds[1];

        Assert.InRange(slowSpeed, 2f, 21f);
        Assert.Equal(slowSpeed, runs.FastDocking.Speeds[1], 1.5f);
    }

    private static string ShipTypes(FrameRateRun run)
        => string.Join(
            ", ",
            run.Harness.Resolve<Universe>().GetAllObjects().Select(o => o.Id).Order());

    private static float DistanceToPlanet(FrameRateRun run)
    {
        foreach (IObject obj in run.Harness.Resolve<Universe>().GetAllObjects())
        {
            if (obj.Id == ObjectIds.Planet)
            {
                return obj.Location.Length();
            }
        }

        return 0;
    }
}

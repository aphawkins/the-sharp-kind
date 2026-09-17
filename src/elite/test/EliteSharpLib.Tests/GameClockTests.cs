// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

namespace EliteSharpLib.Tests;

/// <summary>
/// The housekeeping clock, which is the first piece of Elite to be paced in
/// seconds rather than in ticks.
/// </summary>
/// <remarks>
/// These matter more than they look. The golden traces run the game at its
/// own 13.5Hz, where one update is exactly one step and nothing about the
/// clock is exercised at all - so the whole reason it exists, that the
/// housekeeping keeps its real-time rate when the game is updated at some
/// other rate, is only tested here.
/// </remarks>
public class GameClockTests
{
    private const float GameRate = 1f / GameClock.StepsPerSecond;

    [Fact]
    public void AtTheGamesOwnRateEveryUpdateIsExactlyOneStep()
    {
        GameClock clock = new();

        for (int update = 0; update < 1000; update++)
        {
            Assert.Equal(1, clock.Advance(GameRate));
        }
    }

    [Fact]
    public void AtSixtyHertzTenSecondsStillBuysOneHundredAndThirtyFiveSteps()
    {
        // 13.5 a second, whatever the game is updated at: sixty updates a
        // second must not make the shields regenerate four times as fast.
        GameClock clock = new();
        int steps = 0;

        for (int update = 0; update < 600; update++)
        {
            steps += clock.Advance(1f / 60f);
        }

        Assert.Equal(135, steps);
    }

    [Fact]
    public void AtThirtyHertzTheStepsFallWhereTheRateSaysTheyShould()
    {
        // 30 does not divide 13.5 either, so the steps land unevenly - some
        // updates none, some one. Only the total over time is meant to hold.
        GameClock clock = new();
        int steps = 0;

        for (int update = 0; update < 300; update++)
        {
            steps += clock.Advance(1f / 30f);
        }

        Assert.Equal(135, steps);
    }

    [Fact]
    public void AnUpdateWorthSeveralStepsReturnsThemAllAtOnce()
    {
        GameClock clock = new();

        Assert.Equal(5, clock.Advance(5 * GameRate));
    }

    [Fact]
    public void TheUnspentRemainderIsCarriedRatherThanDropped()
    {
        // Half a step twice is one step; dropping the remainder instead would lose a step at any rate that doesn't divide cleanly.
        GameClock clock = new();

        Assert.Equal(0, clock.Advance(GameRate / 2));
        Assert.Equal(1, clock.Advance(GameRate / 2));
    }
}

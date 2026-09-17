// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests;

/// <summary>
/// Motion that used to be a step per tick and is now a speed.
/// </summary>
/// <remarks>
/// The golden traces run at the game's own 13.5Hz, where a tick is worth
/// exactly one and every one of these conversions multiplies by 1 - which is
/// how they stayed bit-identical, and also why they prove nothing about the
/// change. What the conversion is actually for only shows at another rate,
/// and that is what these cover.
/// </remarks>
public class RateIndependentMotionTests
{
    private const float GameRate = 1f / GameClock.StepsPerSecond;

    [Fact]
    public void TwoHalfTicksRollAsFarAsOneWholeTick()
    {
        PlayerShip whole = NewShip(out GameState wholeState);
        PlayerShip halves = NewShip(out GameState halfState);

        wholeState.Clock.BeginUpdate(GameRate);
        whole.IncreaseRoll();

        halfState.Clock.BeginUpdate(GameRate / 2);
        halves.IncreaseRoll();
        halves.IncreaseRoll();

        Assert.Equal(whole.Roll, halves.Roll, 4);
    }

    [Fact]
    public void APartTickStillMovesTheStickPartWay()
    {
        PlayerShip ship = NewShip(out GameState state);

        state.Clock.BeginUpdate(GameRate / 4);
        ship.IncreasePitch();

        Assert.Equal(0.25f, ship.Pitch, 4);
    }

    [Fact]
    public void LevellingOutStopsAtCentreRatherThanCrossingIt()
    {
        // A whole tick could never overshoot since roll arrives in whole units; a third of a tick can, hence the clamp.
        PlayerShip ship = NewShip(out GameState state);
        ship.Roll = 0.2f;
        ship.IsRolling = false;
        ship.IsPitching = true;

        state.Clock.BeginUpdate(GameRate);
        ship.LevelOut();

        Assert.Equal(0f, ship.Roll);
    }

    [Fact]
    public void LevellingOutFromNegativeAlsoStopsAtCentre()
    {
        PlayerShip ship = NewShip(out GameState state);
        ship.Roll = -0.2f;
        ship.IsRolling = false;
        ship.IsPitching = true;

        state.Clock.BeginUpdate(GameRate);
        ship.LevelOut();

        Assert.Equal(0f, ship.Roll);
    }

    [Fact]
    public void TheStickStillStopsAtItsLimit()
    {
        // Scaling the step must not scale the clamp away with it.
        PlayerShip ship = NewShip(out GameState state);

        state.Clock.BeginUpdate(GameRate);
        for (int update = 0; update < 200; update++)
        {
            ship.IncreaseRoll();
        }

        Assert.Equal(ship.MaxRoll, ship.Roll);
    }

    private static PlayerShip NewShip(out GameState gameState)
    {
        ScreenManager<Screen, IScreenController> views = new(new FakeKeyboard());
        gameState = new(views, TestMissions.Registry());
        return new(gameState);
    }
}

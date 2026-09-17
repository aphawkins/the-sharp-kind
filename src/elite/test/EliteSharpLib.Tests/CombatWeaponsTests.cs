// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Conflict;
using EliteSharpLib.Fakes;
using EliteSharpLib.Lasers;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Audio;
using SharpKind.Fakes;
using SharpKind.Fakes.Audio;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests;

// The player's side of Combat: arming, firing, and ECM; AI tactics live in CombatTests.
public class CombatWeaponsTests
{
    [Fact]
    public void AMissileCannotBeArmedWithNoneInTheRack()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out _);
        ship.MissileCount = 0;

        combat.ArmMissile();

        Assert.False(combat.IsMissileArmed);
    }

    [Fact]
    public void AMissileArmsWhenThereIsOneToArm()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out _);
        ship.MissileCount = 1;

        combat.ArmMissile();

        Assert.True(combat.IsMissileArmed);
    }

    [Fact]
    public void AnArmedMissileLocksOntoAShipInTheCrosshairs()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out _);
        ship.MissileCount = 1;
        combat.ArmMissile();
        FakeShip target = InTheCrosshairs();

        combat.CheckTarget(target, target);

        Assert.Same(target, combat.MissileTarget);
    }

    [Fact]
    public void AnUnarmedMissileLocksOntoNothing()
    {
        Combat combat = CreateCombat(out _, out _, out _);
        FakeShip target = InTheCrosshairs();

        combat.CheckTarget(target, target);

        Assert.Null(combat.MissileTarget);
    }

    [Fact]
    public void TheLockStaysOnTheFirstShipItFound()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out _);
        ship.MissileCount = 1;
        combat.ArmMissile();
        FakeShip first = InTheCrosshairs();
        FakeShip second = InTheCrosshairs();

        combat.CheckTarget(first, first);
        combat.CheckTarget(second, second);

        Assert.Same(first, combat.MissileTarget);
    }

    [Fact]
    public void AShipOutsideTheCrosshairsIsNotLockedOnto()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out _);
        ship.MissileCount = 1;
        combat.ArmMissile();
        FakeShip target = InTheCrosshairs();
        target.Location = new(500, 500, 1000, 0);

        combat.CheckTarget(target, target);

        Assert.Null(combat.MissileTarget);
    }

    [Fact]
    public void UnarmingForgetsTheLock()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out _);
        ship.MissileCount = 1;
        combat.ArmMissile();
        FakeShip target = InTheCrosshairs();
        combat.CheckTarget(target, target);

        combat.UnarmMissile();

        Assert.Null(combat.MissileTarget);
        Assert.False(combat.IsMissileArmed);
    }

    [Fact]
    public void NothingIsFiredWithoutALock()
    {
        Combat combat = CreateCombat(out Universe universe, out PlayerShip ship, out _);
        ship.MissileCount = 1;
        combat.ArmMissile();

        combat.FireMissile();

        Assert.Empty(universe.GetAllObjects());
        Assert.Equal(1, ship.MissileCount);
    }

    [Fact]
    public void FiringSpendsTheMissileAngersItsTargetAndLeavesTheRackUnarmed()
    {
        Combat combat = CreateCombat(out Universe universe, out PlayerShip ship, out _);
        ship.MissileCount = 2;
        combat.ArmMissile();
        FakeShip target = InTheCrosshairs();
        combat.CheckTarget(target, target);

        combat.FireMissile();

        Assert.Single(universe.GetAllObjects());
        Assert.Equal(1, ship.MissileCount);
        Assert.False(combat.IsMissileArmed);
        Assert.Null(combat.MissileTarget);
        Assert.True(target.Flags.HasFlag(ShipProperties.Angry));
    }

    // Named, not passed as the enum: Screen is internal, a public test method can't take it.
    [Theory]
    [InlineData("FrontView")]
    [InlineData("RearView")]
    [InlineData("LeftView")]
    [InlineData("RightView")]
    public void EachViewFiresTheLaserOnItsOwnMount(string viewName)
    {
        Screen screen = Enum.Parse<Screen>(viewName);
        Combat combat = CreateCombat(out _, out PlayerShip ship, out GameState gameState);
        gameState.SetView(screen);
        FitPulseLaserOn(ship, screen);

        Assert.True(combat.FireLaser());
    }

    [Theory]
    [InlineData("FrontView")]
    [InlineData("RearView")]
    [InlineData("LeftView")]
    [InlineData("RightView")]
    public void AMountWithNoLaserOnItFiresNothing(string viewName)
    {
        Combat combat = CreateCombat(out _, out _, out GameState gameState);
        gameState.SetView(Enum.Parse<Screen>(viewName));

        Assert.False(combat.FireLaser());
    }

    // The lasers only fire out of the four views. A chart is not a gun sight.
    [Fact]
    public void AScreenThatIsNotAViewFiresNothing()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out GameState gameState);
        ship.LaserFront = new PulseLaser();
        gameState.SetView(Screen.GalacticChart);

        Assert.False(combat.FireLaser());
    }

    [Fact]
    public void ADockedShipFiresNothing()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out GameState gameState);
        ship.LaserFront = new PulseLaser();
        gameState.SetView(Screen.FrontView);
        gameState.IsDocked = true;

        Assert.False(combat.FireLaser());
    }

    [Fact]
    public void AnOverheatedLaserFiresNothing()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out GameState gameState);
        ship.LaserFront = new PulseLaser();
        gameState.SetView(Screen.FrontView);
        gameState.LaserTemp = GameState.LaserTempOverheated;

        Assert.False(combat.FireLaser());
    }

    [Fact]
    public void FiringHeatsTheLaserAndDrawsOnTheShipsEnergy()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out GameState gameState);
        ship.LaserFront = new PulseLaser();
        gameState.SetView(Screen.FrontView);
        float energy = ship.Energy;

        Assert.True(combat.FireLaser());

        Assert.Equal(GameState.LaserTempMin + GameState.LaserTempPerShot, gameState.LaserTemp);
        Assert.True(ship.Energy < energy);
    }

    [Fact]
    public void AShotThatLandsTakesEnergyOffWhatItHit()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out GameState gameState);
        ship.LaserFront = new MilitaryLaser();
        gameState.SetView(Screen.FrontView);
        _ = combat.FireLaser();
        FakeShip target = InTheCrosshairs();
        target.Energy = 1000;

        combat.CheckTarget(target, target);

        Assert.True(target.Energy < 1000);
        Assert.True(target.Flags.HasFlag(ShipProperties.Angry));
    }

    [Fact]
    public void AStationShrugsOffALaserButTakesOffence()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out GameState gameState);
        ship.LaserFront = new MilitaryLaser();
        gameState.SetView(Screen.FrontView);
        _ = combat.FireLaser();
        FakeShip station = InTheCrosshairs();
        station.Energy = 1000;
        station.Flags |= ShipProperties.Station;

        combat.CheckTarget(station, station);

        Assert.Equal(1000, station.Energy);
        Assert.True(station.Flags.HasFlag(ShipProperties.Angry));
    }

    // The two ships the original armours: only a military laser gets through.
    [Theory]
    [InlineData("Constrictor")]
    [InlineData("Cougar")]
    public void OnlyAMilitaryLaserHurtsTheArmouredShips(string shipName)
    {
        Combat pulse = CreateCombat(out _, out PlayerShip pulseShip, out GameState pulseState);
        pulseShip.LaserFront = new PulseLaser();
        pulseState.SetView(Screen.FrontView);
        _ = pulse.FireLaser();
        FakeShip shrugged = InTheCrosshairs();
        shrugged.Id = shipName;
        shrugged.Energy = 1000;

        pulse.CheckTarget(shrugged, shrugged);

        Assert.Equal(1000, shrugged.Energy);

        Combat military = CreateCombat(out _, out PlayerShip militaryShip, out GameState militaryState);
        militaryShip.LaserFront = new MilitaryLaser();
        militaryState.SetView(Screen.FrontView);
        _ = military.FireLaser();
        FakeShip hurt = InTheCrosshairs();
        hurt.Id = shipName;
        hurt.Energy = 1000;

        military.CheckTarget(hurt, hurt);

        Assert.True(hurt.Energy < 1000);
    }

    [Fact]
    public void AnEcmBurstOnlyStartsWhenThereIsNotOneRunning()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out _);

        combat.ActivateECM(ours: true);
        ship.EcmActive = 4;
        combat.ActivateECM(ours: true);

        // A second press must not refill the burst and give a free permanent ECM.
        Assert.Equal(4, ship.EcmActive);
    }

    [Fact]
    public void SomeoneElsesEcmCostsThePlayerNothing()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out GameState gameState);
        combat.ActivateECM(ours: false);
        float energy = ship.Energy;

        gameState.Clock.BeginUpdate(1f / GameClock.StepsPerSecond);
        combat.TimeECM();

        Assert.True(ship.EcmActive < 32);
        Assert.Equal(energy, ship.Energy);
    }

    [Fact]
    public void ThereIsNothingToTimeWhenNoEcmIsRunning()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out GameState gameState);
        float energy = ship.Energy;

        gameState.Clock.BeginUpdate(1f / GameClock.StepsPerSecond);
        combat.TimeECM();

        Assert.Equal(0, ship.EcmActive);
        Assert.Equal(energy, ship.Energy);
    }

    [Fact]
    public void DockingPutsEveryWeaponBackAsItWasFound()
    {
        Combat combat = CreateCombat(out _, out PlayerShip ship, out GameState gameState);
        ship.MissileCount = 1;
        ship.LaserFront = new PulseLaser();
        gameState.SetView(Screen.FrontView);
        combat.ArmMissile();
        FakeShip target = InTheCrosshairs();
        combat.CheckTarget(target, target);
        _ = combat.FireLaser();
        combat.ActivateECM(ours: true);

        combat.ResetWeapons();

        Assert.Equal(GameState.LaserTempMin, gameState.LaserTemp);
        Assert.Equal(0, ship.EcmActive);
        Assert.Null(combat.MissileTarget);
    }

    [Fact]
    public void LeavingAFightEndsTheBattle()
    {
        Combat combat = CreateCombat(out _, out _, out _);
        combat.InBattle = true;

        combat.Reset();

        Assert.False(combat.InBattle);
    }

    // Inside the ship's own radius, which IsInTarget measures against. Crewed, since that decides whether a ship can be provoked.
    private static FakeShip InTheCrosshairs() => new(new FakeEliteDraw())
    {
        Id = "CobraMk3",
        Traits = ShipTraits.Crewed,
        Location = new(0, 0, 1000, 0),
        Size = 1,
        Energy = 1000,
    };

    private static void FitPulseLaserOn(PlayerShip ship, Screen screen)
    {
        switch (screen)
        {
            case Screen.FrontView:
                ship.LaserFront = new PulseLaser();
                break;

            case Screen.RearView:
                ship.LaserRear = new PulseLaser();
                break;

            case Screen.LeftView:
                ship.LaserLeft = new PulseLaser();
                break;

            default:
                ship.LaserRight = new PulseLaser();
                break;
        }
    }

    private static Combat CreateCombat(out Universe universe, out PlayerShip ship, out GameState gameState)
    {
        ScreenManager<Screen, IScreenController> views = new(new FakeKeyboard());

        // Every screen registered: SetView goes through the manager, which can't reach an unregistered screen.
        foreach (Screen screen in Enum.GetValues<Screen>())
        {
            views.Add(screen, new NothingScreen());
        }

        // A fresh GameState starts docked, where no laser fires at all.
        gameState = new(views, TestMissions.Registry()) { IsDocked = false };
        ship = new PlayerShip(gameState);
        Trade trade = TestGoods.Trade(gameState, ship);
        FakeEliteDraw draw = new();
        RNG rng = new(new FakeRandomSource());
        FakeShipFactory shipFactory = new(draw);
        universe = new Universe(shipFactory, rng);
        AudioController audio = new(new FakeSound(), new Dictionary<string, SfxSample>(), new());
        Pilot pilot = new(draw, audio, universe, ship, gameState);

        return new Combat(
            gameState,
            audio,
            ship,
            trade,
            pilot,
            universe,
            draw,
            new SixteenBitRendition(),
            shipFactory,
            rng,
            TestMissions.Runner(gameState, ship, trade));
    }

    private sealed class NothingScreen : IScreenController
    {
        public void Draw()
        {
        }

        public void HandleInput()
        {
        }

        public void Reset()
        {
        }

        public void Update()
        {
        }
    }
}

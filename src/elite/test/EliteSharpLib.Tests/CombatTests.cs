// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using System.Reflection;
using EliteSharp.Abstractions.Ships;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Conflict;
using EliteSharpLib.Fakes;
using EliteSharpLib.Missions;
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

public class CombatTests
{
    [Fact]
    public void CreateThargoidLaunchesTharglet()
    {
        // Arrange: forces the ~1-in-256 escort roll (RNG.Random(256) > 64) deterministically.
        Combat combat = CreateCombat(out Universe universe, out _, out _, out _, randomValue: 254);

        // Act
        combat.CreateThargoid();

        // Assert: the Thargoid plus its Tharglet escort are both in the universe.
        Assert.Equal(2, universe.GetAllObjects().Count());
    }

    [Fact]
    public void CreateThargoidDoesNotLaunchThargletBelowThreshold()
    {
        // Arrange: force the same roll to miss the threshold.
        Combat combat = CreateCombat(out Universe universe, out _, out _, out _, randomValue: 0);

        // Act
        combat.CreateThargoid();

        // Assert: only the Thargoid itself is in the universe.
        Assert.Single(universe.GetAllObjects());
    }

    [Fact]
    public void ScoopingWithAFullHoldDestroysTheCanisterWithoutDamage()
    {
        // Arrange: original MA59 - full hold destroys the canister without OOPS damage.
        Combat combat = CreateCombat(out _, out PlayerShip ship, out _, out _, randomValue: 0);
        ship.HasFuelScoop = true;
        ship.CargoCapacity = 0;
        ship.ShieldFront = PlayerShip.ShieldMax;
        ship.ShieldRear = PlayerShip.ShieldMax;
        FakeShip canister = new(new FakeEliteDraw())
        {
            Id = "CargoCannister",
            Location = new(0, -100, 500, 0),
        };

        combat.ScoopItem(canister);

        Assert.Equal(PlayerShip.ShieldMax, ship.ShieldFront);
        Assert.Equal(PlayerShip.ShieldMax, ship.ShieldRear);
        Assert.True(canister.Flags.HasFlag(ShipProperties.Dead));
    }

    [Fact]
    public void ScoopingWithNoFuelScoopFittedTakesCollisionDamage()
    {
        // Arrange: original MA58 - no scoop means a genuine collision, full OOPS damage.
        Combat combat = CreateCombat(out _, out PlayerShip ship, out _, out _, randomValue: 0);
        ship.HasFuelScoop = false;
        ship.CargoCapacity = 100;
        ship.ShieldFront = PlayerShip.ShieldMax;
        FakeShip canister = new(new FakeEliteDraw())
        {
            Id = "CargoCannister",
            Location = new(0, -100, 500, 0),
        };

        combat.ScoopItem(canister);

        Assert.True(ship.ShieldFront < PlayerShip.ShieldMax);
    }

    [Fact]
    public void DestroyingAnAsteroidWithAPulseLaserYieldsAlloyAndCargoButNoSplinters()
    {
        // Arrange: original CMP #Mlas - splinters only from a mining-laser kill; alloy/cargo spawn regardless.
        Combat combat = CreateCombat(out Universe universe, out _, out _, out _, randomValue: 2);
        SetLaserType(combat, LaserType.Pulse);
        FakeShip asteroid = new(new FakeEliteDraw()) { Id = ObjectIds.Asteroid, LootMax = 15 };

        InvokeDestroyTarget(combat, asteroid);

        // Alloy (2) and cargo (2) still spawn; no rock splinters.
        Assert.Equal(4, universe.GetAllObjects().Count());
    }

    [Fact]
    public void DestroyingAnAsteroidWithAMiningLaserYieldsSplintersAlloyAndCargo()
    {
        // Arrange: mining-laser kill adds splinters on top of the usual alloy/cargo.
        Combat combat = CreateCombat(out Universe universe, out _, out _, out _, randomValue: 2);
        SetLaserType(combat, LaserType.Mining);
        FakeShip asteroid = new(new FakeEliteDraw()) { Id = ObjectIds.Asteroid, LootMax = 15 };

        InvokeDestroyTarget(combat, asteroid);

        // Splinters (2) plus alloy (2) plus cargo (2).
        Assert.Equal(6, universe.GetAllObjects().Count());
    }

    [Fact]
    public void FiringTacticsSetsFiringAndHostileRegardlessOfHitChance()
    {
        // Arrange: original TACTICS/CPX #160 - one gate governs both firing and the Firing flag. -0.90 sits between the dead zone and the hit threshold.
        Combat combat = CreateCombat(out _, out PlayerShip ship, out _, out _, randomValue: 0);
        ship.ShieldFront = PlayerShip.ShieldMax;
        FakeShip enemy = new(new FakeEliteDraw()) { LaserStrength = 10 };

        InvokeFiringTactics(combat, enemy, -0.90f, new(0, 0, 1, 0));

        Assert.True(enemy.Flags.HasFlag(ShipProperties.Firing));
        Assert.True(enemy.Flags.HasFlag(ShipProperties.Hostile));
        Assert.Equal(PlayerShip.ShieldMax, ship.ShieldFront);
    }

    [Fact]
    public void AttackTacticsDoesNotFireBetweenTheOldAndNewThreshold()
    {
        // Arrange: -0.86 sits below the re-derived -0.889 (-32/36) gate, so tactics must not fire.
        Combat combat = CreateCombat(out Universe universe, out _, out _, out _, randomValue: 0);
        FakeShip enemy = new(new FakeEliteDraw())
        {
            Id = "CobraMk3",
            Flags = ShipProperties.Angry,
            Rotmat = Matrix4x4.Identity,
            Location = new(0.51f, 0, -0.86f, 0),
            LaserStrength = 10,
            Energy = 100,
            EnergyMax = 100,
        };
        universe.AddNewShip(enemy, enemy.Location, enemy.Rotmat, 0, 0);

        combat.Tactics(enemy, 0);

        Assert.False(enemy.Flags.HasFlag(ShipProperties.Firing));
    }

    [Fact]
    public void PoliceIgnoreLegalStatusWithNoPoliceNearby()
    {
        // Arrange: original LDX MANY+COPS; BEQ P%+5 - no cops in the bubble means legal status isn't ORed in yet.
        Combat combat = CreateCombat(out Universe universe, out _, out _, out GameState gameState, randomValue: 50);
        gameState.Cmdr.LegalStatus = 200;

        InvokeCheckForPolice(combat);

        Assert.Empty(universe.GetAllObjects());
    }

    [Fact]
    public void PoliceFactorInLegalStatusWhenAlreadyPresent()
    {
        // Arrange: with cops already in the bubble, legal status is ORed into the spawn chance.
        Combat combat = CreateCombat(out Universe universe, out _, out _, out GameState gameState, randomValue: 50);
        gameState.Cmdr.LegalStatus = 200;
        FakeShip existingPolice = new(new FakeEliteDraw()) { Id = ObjectIds.Viper };
        universe.AddNewShip(existingPolice, default, Matrix4x4.Identity, 0, 0);

        InvokeCheckForPolice(combat);

        // The existing Viper plus a newly spawned police ship.
        Assert.Equal(2, universe.GetAllObjects().Count());
    }

    [Fact]
    public void ABountyHunterLoneWolfIsNotHostileBelowLegalStatus40()
    {
        // Arrange: original TACTICS part 3 - a bounty hunter (NEWB bit 1) turns hostile only once FIST >= 40.
        Combat combat = CreateCombat(out Universe universe, out _, out _, out GameState gameState, randomValue: 0);
        gameState.Cmdr.LegalStatus = 39;

        InvokeCreateLoneWolf(combat);

        Assert.False(Assert.Single(universe.GetAllObjects()).Flags.HasFlag(ShipProperties.Angry));
    }

    [Fact]
    public void ABountyHunterLoneWolfIsHostileAtLegalStatus40()
    {
        Combat combat = CreateCombat(out Universe universe, out _, out _, out GameState gameState, randomValue: 0);
        gameState.Cmdr.LegalStatus = 40;

        InvokeCreateLoneWolf(combat);

        Assert.True(Assert.Single(universe.GetAllObjects()).Flags.HasFlag(ShipProperties.Angry));
    }

    [Fact]
    public void AnEcmBurstLastsThirtyTwoTicksWhateverTheUpdateRate()
    {
        // Pins burst length to time, not update count: quarter-tick updates take 4x as many updates for the same 32 ticks.
        Combat combat = CreateCombat(out _, out PlayerShip ship, out _, out GameState state, randomValue: 0);
        ship.EcmActive = 32;
        state.Clock.BeginUpdate(1f / GameClock.StepsPerSecond / 4f);

        for (int update = 0; update < (32 * 4) - 1; update++)
        {
            combat.TimeECM();
        }

        Assert.True(ship.EcmActive > 0);

        combat.TimeECM();

        Assert.Equal(0f, ship.EcmActive, 4);
    }

    [Fact]
    public void AnEcmBurstCostsTheSameEnergyWhateverTheUpdateRate()
    {
        // Drain must follow the same clock as the countdown, or a faster rate makes E.C.M. cheap.
        Combat combat = CreateCombat(out _, out PlayerShip whole, out _, out GameState wholeState, randomValue: 0);
        float startingEnergy = whole.Energy;
        whole.EcmActive = 32;
        wholeState.Clock.BeginUpdate(1f / GameClock.StepsPerSecond);
        for (int update = 0; update < 32; update++)
        {
            combat.TimeECM();
        }

        float wholeDrain = startingEnergy - whole.Energy;

        Combat quarters = CreateCombat(out _, out PlayerShip fast, out _, out GameState fastState, randomValue: 0);
        fast.EcmActive = 32;
        fastState.Clock.BeginUpdate(1f / GameClock.StepsPerSecond / 4f);
        for (int update = 0; update < 32 * 4; update++)
        {
            quarters.TimeECM();
        }

        Assert.Equal(wholeDrain, startingEnergy - fast.Energy, wholeDrain * 0.001f);
    }

    [Fact]
    public void AMissileAsksForAccelerationInProportionToTheTick()
    {
        // Missiles steer every update, not one-in-eight, so acceleration must be expressed as a rate.
        Combat combat = CreateCombat(out Universe universe, out _, out _, out GameState state, randomValue: 0);
        FakeShip whole = LaunchedMissile(universe);
        FakeShip quarter = LaunchedMissile(universe);

        state.Clock.BeginUpdate(1f / GameClock.StepsPerSecond);
        combat.Tactics(whole, 0);

        state.Clock.BeginUpdate(1f / GameClock.StepsPerSecond / 4f);
        combat.Tactics(quarter, 1);

        Assert.NotEqual(0f, whole.Acceleration);
        Assert.Equal(whole.Acceleration / 4f, quarter.Acceleration, 0.001f);
    }

    private static FakeShip LaunchedMissile(Universe universe)
    {
        FakeShip missile = new(new FakeEliteDraw()) { Id = "Missile", Flags = ShipProperties.Missile };
        missile.Flags |= ShipProperties.Angry;
        universe.AddNewShip(missile, new(0, 0, 500, 0), Matrix4x4.Identity, 0, 0);
        return missile;
    }

    private static void InvokeCreateLoneWolf(Combat combat)
    {
        MethodInfo method = typeof(Combat).GetMethod("CreateLoneWolf", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(Combat), "CreateLoneWolf");
        method.Invoke(combat, null);
    }

    private static void InvokeCheckForPolice(Combat combat)
    {
        MethodInfo method = typeof(Combat).GetMethod("CheckForPolice", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(Combat), "CheckForPolice");
        method.Invoke(combat, null);
    }

    private static void SetLaserType(Combat combat, LaserType laserType)
    {
        FieldInfo field = typeof(Combat).GetField("_laserType", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(Combat), "_laserType");
        field.SetValue(combat, laserType);
    }

    private static void InvokeDestroyTarget(Combat combat, IShip obj)
    {
        MethodInfo method = typeof(Combat).GetMethod("DestroyTarget", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(Combat), "DestroyTarget");
        method.Invoke(combat, [obj]);
    }

    private static void InvokeFiringTactics(Combat combat, IShip ship, float direction, Vector4 nvec)
    {
        MethodInfo method = typeof(Combat).GetMethod("FiringTactics", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(Combat), "FiringTactics");
        method.Invoke(combat, [ship, direction, nvec]);
    }

    private static Combat CreateCombat(
        out Universe universe, out PlayerShip ship, out Trade trade, out GameState gameState, int randomValue)
    {
        ScreenManager<Screen, IScreenController> views = new(new FakeKeyboard());
        gameState = new(views, TestMissions.Registry());
        ship = new PlayerShip(gameState);
        trade = TestGoods.Trade(gameState, ship);
        FakeEliteDraw draw = new();
        RNG rng = new(new FakeRandomSource { RandomValue = randomValue });
        FakeShipFactory shipFactory = new(draw);
        universe = new(shipFactory, rng);
        AudioController audio = new(new FakeSound(), new Dictionary<string, SfxSample>(), new());
        Pilot pilot = new(draw, audio, universe, ship, gameState);

        MissionRunner missions = TestMissions.Runner(gameState, ship, trade);

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
            missions);
    }
}

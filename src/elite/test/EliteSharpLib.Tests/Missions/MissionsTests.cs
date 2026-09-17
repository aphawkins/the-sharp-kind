// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Missions;
using EliteSharp.Missions.Classic;
using EliteSharpLib.Types;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests.Missions;

// The two built-in missions, asked the way a plugin would be asked: through
// IMissionContext and nothing else.
public class MissionsTests
{
    // The one translation the port had to make: the original compared seed bytes, which a mission can't see, so this checks the numbers still name the right systems.
    [Theory]
    [InlineData(1, 193, "ORARRA")]
    [InlineData(2, 83, "CEERDI")]
    [InlineData(2, 36, "BIRERA")]
    public void TheNumberedSystemsAreTheOnesTheMissionsMean(int galaxyNumber, int planetNumber, string expected)
    {
        // Arrange
        GameState gameState = CreateGameState();
        PlanetController planet = new(gameState);

        // Act
        GalaxySeed seed = planet.PlanetAt(TestMissions.GalaxyAt(galaxyNumber), planetNumber);

        // Assert
        Assert.Equal(expected, planet.NamePlanet(seed));
    }

    [Fact]
    public void TheConstrictorIsNotOfferedBelowAboveAverage()
    {
        // Arrange
        ConstrictorMission mission = new();

        // Act & Assert
        Assert.Null(mission.Advance(new FakeContext { CombatScore = 255 }, ConstrictorMission.None));
        Assert.NotNull(mission.Advance(new FakeContext { CombatScore = 256 }, ConstrictorMission.None));
    }

    [Fact]
    public void TheConstrictorIsNotOfferedOnceItsGalaxiesAreBehindYou()
    {
        // Arrange
        ConstrictorMission mission = new();

        // Act & Assert
        Assert.Null(mission.Advance(new FakeContext { CombatScore = 256, GalaxyNumber = 2 }, ConstrictorMission.None));
    }

    [Fact]
    public void TheConstrictorBriefBringsTheShipItIsAbout()
    {
        // Arrange: the ship posing behind the text is named by the briefing, so
        // the screen spawns it without knowing which mission it belongs to.
        ConstrictorMission mission = new();

        // Act
        MissionStep? step = mission.Advance(new FakeContext { CombatScore = 256 }, ConstrictorMission.None);

        // Assert
        Assert.Equal("Constrictor", step?.Briefing?.ShipName);
        Assert.Null(step?.Award);
    }

    [Fact]
    public void TheConstrictorDebriefPaysTheBounty()
    {
        // Arrange
        ConstrictorMission mission = new();

        // Act
        MissionStep? step = mission.Advance(new FakeContext(), ConstrictorMission.Destroyed);

        // Assert
        Assert.Equal(ConstrictorMission.Rewarded, step?.Stage);
        Assert.Equal(256, step?.Award?.CombatScore);
        Assert.Equal(5000, step?.Award?.Credits);
        Assert.Null(step?.Award?.Equipment);
    }

    [Fact]
    public void OnlyTheConstrictorsOwnKillMovesIt()
    {
        // Arrange
        ConstrictorMission mission = new();
        FakeContext context = new();

        // Act & Assert
        Assert.Null(mission.ShipDestroyed(context, ConstrictorMission.Briefed, "Thargoid"));
        Assert.Null(mission.ShipDestroyed(context, ConstrictorMission.None, "Constrictor"));
        Assert.Equal(
            ConstrictorMission.Destroyed,
            mission.ShipDestroyed(context, ConstrictorMission.Briefed, "Constrictor")?.Stage);
    }

    [Fact]
    public void TheStolenShipOnlyWaitsInTheSystemItIsHidingIn()
    {
        // Arrange
        ConstrictorMission mission = new();

        // Act
        LoneWolfEncounter? here = mission.LoneWolfSubstitute(
            new FakeContext { GalaxyNumber = 1, CurrentPlanetNumber = 193 },
            ConstrictorMission.Briefed);
        LoneWolfEncounter? elsewhere = mission.LoneWolfSubstitute(
            new FakeContext { GalaxyNumber = 1, CurrentPlanetNumber = 192 },
            ConstrictorMission.Briefed);

        // Assert
        Assert.Equal("Constrictor", here?.ShipName);
        Assert.True(here?.Unique);
        Assert.Null(elsewhere);
    }

    [Fact]
    public void TheRumourIsWhatTheyAreSayingHereRatherThanAboutAnywhereChosen()
    {
        // The bug this fixes: the rumour used to print for whichever system the data screen happened to show, not the one the story is about.
        ConstrictorMission mission = new();
        FakeContext atReesdice = new() { IsDocked = true, CurrentPlanetNumber = 150 };
        FakeContext elsewhere = new() { IsDocked = true, CurrentPlanetNumber = 42 };

        // Act & Assert
        Assert.NotNull(mission.DescribePlanet(atReesdice, ConstrictorMission.Briefed, 150));
        Assert.Null(mission.DescribePlanet(elsewhere, ConstrictorMission.Briefed, 150));
    }

    [Fact]
    public void NobodyTalksAboutTheStolenShipInFlight()
    {
        // Arrange
        ConstrictorMission mission = new();
        FakeContext inFlight = new() { IsDocked = false, CurrentPlanetNumber = 150 };

        // Act & Assert
        Assert.Null(mission.DescribePlanet(inFlight, ConstrictorMission.Briefed, 150));
    }

    [Fact]
    public void NobodyTalksAboutTheStolenShipBeforeOrAfterTheHunt()
    {
        // Arrange
        ConstrictorMission mission = new();
        FakeContext docked = new() { IsDocked = true, CurrentPlanetNumber = 150 };

        // Act & Assert
        Assert.Null(mission.DescribePlanet(docked, ConstrictorMission.None, 150));
        Assert.Null(mission.DescribePlanet(docked, ConstrictorMission.Rewarded, 150));
    }

    [Fact]
    public void TheNavyOnlyCallsOnceTheConstrictorHasBeenPaidFor()
    {
        // Arrange: the one thing either mission knows about the other, asked
        // the only way a mission can - by name.
        ThargoidMission mission = new();

        // Act & Assert
        Assert.Null(mission.Advance(
            new FakeContext { CombatScore = 1280, GalaxyNumber = 2, Stage = ConstrictorMission.Briefed },
            ThargoidMission.None));
        Assert.NotNull(mission.Advance(
            new FakeContext { CombatScore = 1280, GalaxyNumber = 2, Stage = ConstrictorMission.Rewarded },
            ThargoidMission.None));
    }

    [Fact]
    public void TheDebriefFitsTheEnergyUnitRatherThanPayingForOne()
    {
        // Arrange
        ThargoidMission mission = new();

        // Act
        MissionStep? step = mission.Advance(
            new FakeContext { GalaxyNumber = 2, CurrentPlanetNumber = 36 },
            ThargoidMission.CarryingPlans);

        // Assert
        Assert.Equal(MissionEquipment.NavalEnergyUnit, step?.Award?.Equipment);
        Assert.Equal(0, step?.Award?.Credits);
    }

    [Fact]
    public void TheThargoidsOnlyComeForACommanderCarryingThePlans()
    {
        // Arrange
        ThargoidMission mission = new();
        FakeContext context = new();

        // Act & Assert
        Assert.Null(mission.Ambush(context, ThargoidMission.Summoned));
        Assert.Equal("Thargoid", mission.Ambush(context, ThargoidMission.CarryingPlans)?.ShipName);
    }

    private static GameState CreateGameState() => new(
        new ScreenManager<Screen, IScreenController>(new FakeKeyboard()),
        TestMissions.Registry());

    private sealed class FakeContext : IMissionContext
    {
        public int CombatScore { get; init; }

        public int GalaxyNumber { get; init; }

        public int CurrentPlanetNumber { get; init; } = -1;

        public bool IsDocked { get; init; }

        /// <summary>
        /// Gets what every other mission is at, which is all one mission can
        /// ask about another.
        /// </summary>
        public string? Stage { get; init; }

        public string? StageOf(string missionName) => Stage;
    }
}

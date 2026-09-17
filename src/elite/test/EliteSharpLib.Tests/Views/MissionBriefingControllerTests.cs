// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharp.Missions.Classic;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Conflict;
using EliteSharpLib.Equipment;
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

namespace EliteSharpLib.Tests.Views;

// One view draws every briefing; what tells them apart is a headline, paragraph count and portrait.
public class MissionBriefingControllerTests
{
    // The galaxy the Thargoid run happens in, and the two systems it runs
    // between, by the numbers that galaxy gives them.
    private const int ThargoidGalaxy = 2;
    private const int Ceerdi = 83;
    private const int Birera = 36;

    private readonly FakeKeyboard _keyboard = new();
    private MissionBriefingController? _controller;

    [Fact]
    public void LeavesAtOnceWhenNoMissionHasAnythingToSay()
    {
        // Arrange: what happens on almost every docking.
        MissionBriefingController controller = CreateController(out GameState gameState);

        // Act
        controller.Reset();

        // Assert
        Assert.Empty(controller.Briefing.Paragraphs);
        Assert.Equal(Screen.CommanderStatus, gameState.CurrentScreen);
    }

    [Fact]
    public void TheConstrictorBriefIsTwoParagraphsAndNoHeadline()
    {
        // Arrange
        MissionBriefingController controller = CreateController(out GameState gameState);
        GivenTheConstrictorIsOffered(gameState);

        // Act
        controller.Reset();

        // Assert
        Assert.Equal(2, controller.Briefing.Paragraphs.Count);
        Assert.False(controller.Briefing.HasHeadline);
        Assert.False(controller.Briefing.ShowPortrait);
    }

    [Fact]
    public void TheFirstGalaxyIsToldWhereTheConstrictorWasSeen()
    {
        // Arrange
        MissionBriefingController controller = CreateController(out GameState gameState);
        GivenTheConstrictorIsOffered(gameState);
        gameState.Cmdr.GalaxyNumber = 0;

        // Act
        controller.Reset();

        // Assert
        Assert.Contains("Reesdice", controller.Briefing.Paragraphs[1], StringComparison.Ordinal);
    }

    [Fact]
    public void LaterGalaxiesAreToldItJumpedHere()
    {
        // Arrange
        MissionBriefingController controller = CreateController(out GameState gameState);
        GivenTheConstrictorIsOffered(gameState);
        gameState.Cmdr.GalaxyNumber = 1;

        // Act
        controller.Reset();

        // Assert
        string paragraph = controller.Briefing.Paragraphs[1];
        Assert.Contains("jumped to this galaxy", paragraph, StringComparison.Ordinal);
        Assert.DoesNotContain("Reesdice", paragraph, StringComparison.Ordinal);
    }

    [Fact]
    public void TheConstrictorDebriefCarriesAHeadlineAndPaysTheBounty()
    {
        // Arrange
        MissionBriefingController controller = CreateController(out GameState gameState, out Trade trade);
        gameState.Cmdr.Missions.MoveTo(ConstrictorMission.Id, ConstrictorMission.Destroyed);
        float creditsBefore = trade.Credits;

        // Act
        controller.Reset();

        // Assert
        Assert.Equal("Congratulations Commander!", controller.Briefing.Headline);
        Assert.Single(controller.Briefing.Paragraphs);
        Assert.Equal(ConstrictorMission.Rewarded, gameState.Cmdr.Missions.StageOf(ConstrictorMission.Id));
        Assert.Equal(creditsBefore + 5000, trade.Credits);
    }

    [Fact]
    public void TheNavysFirstCallIsOneParagraphAndNobodyPictured()
    {
        // Arrange
        MissionBriefingController controller = CreateController(out GameState gameState);
        GivenTheThargoidRunIsOffered(gameState);

        // Act
        controller.Reset();

        // Assert
        Assert.Single(controller.Briefing.Paragraphs);
        Assert.False(controller.Briefing.HasHeadline);
        Assert.False(controller.Briefing.ShowPortrait);
        Assert.Equal(ThargoidMission.Summoned, gameState.Cmdr.Missions.StageOf(ThargoidMission.Id));
    }

    [Fact]
    public void TheAgentHandingOverThePlansIsPictured()
    {
        // Arrange: the one briefing with a portrait, which is the only thing
        // telling the view to leave room for it.
        MissionBriefingController controller = CreateController(out GameState gameState);
        gameState.Cmdr.Missions.MoveTo(ThargoidMission.Id, ThargoidMission.Summoned);
        GivenDockedAt(gameState, Ceerdi);

        // Act
        controller.Reset();

        // Assert
        Assert.Equal(2, controller.Briefing.Paragraphs.Count);
        Assert.True(controller.Briefing.ShowPortrait);
    }

    [Fact]
    public void TheThargoidDebriefCarriesAHeadlineAndFitsTheEnergyUnit()
    {
        // Arrange
        MissionBriefingController controller = CreateController(out GameState gameState, out _, out PlayerShip ship);
        gameState.Cmdr.Missions.MoveTo(ThargoidMission.Id, ThargoidMission.CarryingPlans);
        GivenDockedAt(gameState, Birera);

        // Act
        controller.Reset();

        // Assert
        Assert.Equal("Well done Commander!", controller.Briefing.Headline);
        Assert.Single(controller.Briefing.Paragraphs);
        Assert.Equal(EnergyUnit.Naval, ship.EnergyUnit);
    }

    [Fact]
    public void SpaceShowsTheNextMissionsMessageRatherThanRepeatingTheFirst()
    {
        // One docking can still earn both: the Constrictor's debrief is what makes the Navy call about the Thargoids.
        MissionBriefingController controller = CreateController(out GameState gameState);
        gameState.Cmdr.Missions.MoveTo(ConstrictorMission.Id, ConstrictorMission.Destroyed);
        gameState.Cmdr.Score = 1280 - 256;
        gameState.Cmdr.GalaxyNumber = 2;
        controller.Reset();
        Assert.Equal("Congratulations Commander!", controller.Briefing.Headline);

        // Act: the debrief's own 256 points are what take the commander to
        // Dangerous, so the Navy calls on the very next screen.
        Space();

        // Assert
        Assert.Equal(ThargoidMission.Summoned, gameState.Cmdr.Missions.StageOf(ThargoidMission.Id));
        Assert.Single(controller.Briefing.Paragraphs);
        Assert.False(controller.Briefing.HasHeadline);
    }

    [Fact]
    public void SpaceLeavesTheScreenWhenNoMissionIsLeftToSpeak()
    {
        // Arrange
        MissionBriefingController controller = CreateController(out GameState gameState);
        gameState.Cmdr.Missions.MoveTo(ThargoidMission.Id, ThargoidMission.CarryingPlans);
        GivenDockedAt(gameState, Birera);
        controller.Reset();

        // Act
        Space();

        // Assert
        Assert.Equal(Screen.CommanderStatus, gameState.CurrentScreen);
    }

    // The mission compares planet numbers, so a test can't fake a system by overwriting seed bytes; it has to use the real one.
    private static void GivenDockedAt(GameState gameState, int planetNumber)
    {
        gameState.Cmdr.GalaxyNumber = ThargoidGalaxy;
        gameState.Cmdr.Galaxy = TestMissions.GalaxyAt(ThargoidGalaxy);
        gameState.DockedPlanet = new PlanetController(gameState).PlanetAt(gameState.Cmdr.Galaxy, planetNumber);
    }

    private static void GivenTheConstrictorIsOffered(GameState gameState)
    {
        gameState.Cmdr.Score = 256;
        gameState.Cmdr.GalaxyNumber = 0;
    }

    private static void GivenTheThargoidRunIsOffered(GameState gameState)
    {
        gameState.Cmdr.Missions.MoveTo(ConstrictorMission.Id, ConstrictorMission.Rewarded);
        gameState.Cmdr.Score = 1280;
        gameState.Cmdr.GalaxyNumber = 2;
    }

    private void Space()
    {
        _keyboard.KeyDown(ConsoleKey.Spacebar, ConsoleModifiers.None);
        _controller!.HandleInput();
    }

    private MissionBriefingController CreateController(out GameState gameState)
        => CreateController(out gameState, out _, out _);

    private MissionBriefingController CreateController(out GameState gameState, out Trade trade)
        => CreateController(out gameState, out trade, out _);

    private MissionBriefingController CreateController(
        out GameState gameState,
        out Trade trade,
        out PlayerShip ship)
    {
        ScreenManager<Screen, IScreenController> views = new(_keyboard);
        views.Add(Screen.CommanderStatus, new FakeView());
        gameState = new(views, TestMissions.Registry());
        ship = new(gameState);
        trade = TestGoods.Trade(gameState, ship);
        FakeEliteDraw draw = new();
        RNG rng = new(new FakeRandomSource());
        FakeShipFactory shipFactory = new(draw);
        Universe universe = new(shipFactory, rng);
        AudioController audio = new(new FakeSound(), new Dictionary<string, SfxSample>(), new());
        Pilot pilot = new(draw, audio, universe, ship, gameState);
        MissionRunner missions = TestMissions.Runner(gameState, ship, trade);
        Combat combat = new(
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

        return _controller = new MissionBriefingController(
            gameState,
            _keyboard,
            ship,
            missions,
            combat,
            universe,
            shipFactory,
            new FakeMissionBriefingView());
    }

    private sealed class FakeMissionBriefingView : IMissionBriefingView
    {
        public Vector4 ShipLocation => new(200, 90, 600, 0);

        public void Draw(MissionBriefingModel model)
        {
            // Drawing is not under test here.
        }
    }
}

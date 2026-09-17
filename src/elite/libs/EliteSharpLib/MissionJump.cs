// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Types;
using EliteSharpLib.Views;

namespace EliteSharpLib;

/// <summary>
/// Jumps to the mission briefing screens, which are otherwise reachable only
/// by playing the missions - the Constrictor brief wants a combat rating of
/// Above Average, and the Thargoid sequence wants the third galaxy and two
/// specific planets. Checking those screens' layout meant hours of play or an
/// edited save; with <see cref="EnvVar"/> set, Ctrl-M cycles them (see
/// docs/elite-readme.md).
/// <para>
/// Each jump sets the state the screen's own <c>Reset</c> tests for and then
/// lets that <c>Reset</c> run, rather than forcing the screen straight in:
/// the briefings choose their text - and, for the Constrictor, spawn the ship
/// the screen shows - on the way through, so anything that skipped it would
/// be checking a screen the game never draws.
/// </para>
/// <para>
/// These are cheats, and they leave the commander mid-mission. The last two
/// move the commander to the system the briefing expects, which is now a real
/// jump - the galaxy's seed and the docked planet are both set, so the chart,
/// the planet name and the data screen agree with each other afterwards.
/// Restart the game rather than carrying on from one all the same.
/// </para>
/// </summary>
internal static class MissionJump
{
    /// <summary>
    /// Set (to any value) to enable the Ctrl-M jump - a runtime opt-in rather
    /// than a debug build, following the same convention as
    /// <c>ELITE_DEBUG_COMMANDER</c> (issue #7 took Elite's last
    /// conditional-compilation site out for this reason), so the screens can
    /// be checked in a Release build too. Unset in normal play.
    /// </summary>
    internal const string EnvVar = "ELITE_DEBUG_MISSIONS";

    // Spelled out because these missions are a plugin now: the game does not reference them, and a jump to one nobody installed does nothing.
    private const string Constrictor = "Constrictor";
    private const string Thargoid = "Thargoid";
    private const string NotStarted = "None";
    private const string Destroyed = "Destroyed";
    private const string Rewarded = "Rewarded";
    private const string Summoned = "Summoned";
    private const string CarryingPlans = "CarryingPlans";

    // The Thargoid briefings key off where the commander is docked: Ceerdi for the second brief, Birera for the debrief.
    // Planet numbers as the missions speak them, not seed bytes.
    private const int ThargoidGalaxy = 2;
    private const int Ceerdi = 83;
    private const int Birera = 36;

    // The galaxy seed a new commander starts on; later galaxies are this rotated once each (Space.EnterNextGalaxy).
    private const int FirstGalaxyA = 0x4a;
    private const int FirstGalaxyB = 0x5a;
    private const int FirstGalaxyC = 0x48;
    private const int FirstGalaxyD = 0x02;
    private const int FirstGalaxyE = 0x53;
    private const int FirstGalaxyF = 0xb7;

    // The combat scores the two missions are offered at (MissionBriefingController).
    private const int AboveAverageScore = 256;
    private const int DangerousScore = 1280;

    /// <summary>
    /// Gets a value indicating whether <see cref="EnvVar"/> is set.
    /// </summary>
    internal static bool IsEnabled => Environment.GetEnvironmentVariable(EnvVar) is not null;

    /// <summary>
    /// Gets the number of mission screens <see cref="To"/> can reach.
    /// </summary>
    internal static int Count => 5;

    /// <summary>
    /// Jumps to one of the mission screens, counted from 0 in the order the
    /// missions play out: Constrictor brief, Constrictor debrief, then the
    /// Thargoid sequence's two briefs and its debrief.
    /// </summary>
    /// <param name="gameState">The game to move.</param>
    /// <param name="planet">Used to turn a planet number back into a system.</param>
    /// <param name="stage">Which briefing to jump to.</param>
    internal static void To(GameState gameState, PlanetController planet, int stage)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        ArgumentNullException.ThrowIfNull(planet);

        switch (stage)
        {
            case 0:
                MoveTo(gameState, Constrictor, NotStarted);
                MoveTo(gameState, Thargoid, NotStarted);
                gameState.Cmdr.Score = AboveAverageScore;
                gameState.Cmdr.GalaxyNumber = 0;
                gameState.SetView(Screen.MissionBriefing);
                break;

            case 1:
                MoveTo(gameState, Constrictor, Destroyed);
                gameState.SetView(Screen.MissionBriefing);
                break;

            case 2:
                MoveTo(gameState, Constrictor, Rewarded);
                MoveTo(gameState, Thargoid, NotStarted);
                gameState.Cmdr.Score = DangerousScore;
                gameState.Cmdr.GalaxyNumber = ThargoidGalaxy;
                gameState.Cmdr.Galaxy = GalaxyAt(ThargoidGalaxy);
                gameState.SetView(Screen.MissionBriefing);
                break;

            case 3:
                MoveTo(gameState, Constrictor, Rewarded);
                MoveTo(gameState, Thargoid, Summoned);
                DockAt(gameState, planet, ThargoidGalaxy, Ceerdi);
                gameState.SetView(Screen.MissionBriefing);
                break;

            default:
                MoveTo(gameState, Constrictor, Rewarded);
                MoveTo(gameState, Thargoid, CarryingPlans);
                DockAt(gameState, planet, ThargoidGalaxy, Birera);
                gameState.SetView(Screen.MissionBriefing);
                break;
        }
    }

    /// <summary>
    /// Moves a mission on, if it is installed at all. The missions are a plugin
    /// now, so a cheat that names one has to cope with nobody providing it.
    /// </summary>
    private static void MoveTo(GameState gameState, string mission, string stage)
    {
        if (gameState.Cmdr.Missions.StageOf(mission) is not null)
        {
            gameState.Cmdr.Missions.MoveTo(mission, stage);
        }
    }

    /// <summary>
    /// Puts the commander in a galaxy, docked at one of its systems, as though
    /// they had flown there - the galaxy's seed, the docked planet, its name
    /// and its data all agree afterwards.
    /// </summary>
    private static void DockAt(GameState gameState, PlanetController planet, int galaxyNumber, int planetNumber)
    {
        gameState.Cmdr.GalaxyNumber = galaxyNumber;
        gameState.Cmdr.Galaxy = GalaxyAt(galaxyNumber);
        gameState.DockedPlanet = planet.PlanetAt(gameState.Cmdr.Galaxy, planetNumber);
        gameState.PlanetName = planet.NamePlanet(gameState.DockedPlanet);
        gameState.HyperspacePlanet = new(gameState.DockedPlanet);
        gameState.CurrentPlanetData = PlanetController.GeneratePlanetData(gameState.DockedPlanet);
    }

    /// <summary>
    /// The seed of the numbered galaxy, which is the first one's rotated left
    /// once per galaxy jumped.
    /// </summary>
    private static GalaxySeed GalaxyAt(int galaxyNumber)
    {
        GalaxySeed galaxy = new()
        {
            A = FirstGalaxyA,
            B = FirstGalaxyB,
            C = FirstGalaxyC,
            D = FirstGalaxyD,
            E = FirstGalaxyE,
            F = FirstGalaxyF,
        };

        for (int i = 0; i < galaxyNumber; i++)
        {
            galaxy = new()
            {
                A = RotateByteLeft(galaxy.A),
                B = RotateByteLeft(galaxy.B),
                C = RotateByteLeft(galaxy.C),
                D = RotateByteLeft(galaxy.D),
                E = RotateByteLeft(galaxy.E),
                F = RotateByteLeft(galaxy.F),
            };
        }

        return galaxy;
    }

    private static int RotateByteLeft(int value) => ((value << 1) | ((value >> 7) & 1)) & 0xFF;
}

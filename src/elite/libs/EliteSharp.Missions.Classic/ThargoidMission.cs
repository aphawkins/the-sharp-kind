// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Missions;

namespace EliteSharp.Missions.Classic;

/// <summary>
/// Running the Thargoid defence plans from Ceerdi to Birera, with the
/// Thargoids hunting the commander the whole way. The Navy only calls once the
/// Constrictor has been paid for, which is the one thing either mission knows
/// about the other, and it asks that the only way a mission can: by name.
/// </summary>
public sealed class ThargoidMission : IMission, IMissionEncounters
{
    /// <inheritdoc cref="ConstrictorMission.Id"/>
    internal const string Id = "Thargoid";

    /// <summary>
    /// The mission has not been offered.
    /// </summary>
    internal const string None = "None";

    /// <summary>
    /// The Navy has asked the commander to report to Ceerdi.
    /// </summary>
    internal const string Summoned = "Summoned";

    /// <summary>
    /// The plans were handed over at Ceerdi and must reach Birera.
    /// </summary>
    internal const string CarryingPlans = "CarryingPlans";

    /// <summary>
    /// The plans reached Birera and the Navy energy unit has been fitted.
    /// </summary>
    internal const string Rewarded = "Rewarded";

    /// <summary>
    /// The combat score the Navy calls at, which is where Dangerous starts.
    /// </summary>
    private const int DangerousScore = 1280;

    /// <summary>
    /// The galaxy the run happens in, and the two systems it runs between, as
    /// planet numbers rather than the seed bytes the game used to compare - a
    /// mission cannot see seeds. The game's MissionsTests checks these still name Ceerdi
    /// and Birera.
    /// </summary>
    private const int RunGalaxy = 2;

    /// <inheritdoc cref="RunGalaxy"/>
    private const int Ceerdi = 83;

    /// <inheritdoc cref="RunGalaxy"/>
    private const int Birera = 36;

    /// <summary>
    /// How often the Thargoids find a commander carrying the plans, out of 256
    /// encounter checks.
    /// </summary>
    private const byte AmbushChance = 56;

    private const string BriefA =
        "Attention Commander, I am Captain Fortesque of Her Majesty's Space Navy. "
            + "We have need of your services again. If you would be so good as to go to "
            + "Ceerdi you will be briefed.If succesful, you will be rewarded."
            + "---MESSAGE ENDS.";

    private const string BriefB =
        "Good Day Commander. I am Agent Blake of Naval Intelligence. As you know, "
            + "the Navy have been keeping the Thargoids off your ass out in deep space "
            + "for many years now. Well the situation has changed. Our boys are ready "
            + "for a push right to the home system of those murderers.";

    private const string BriefC =
        "I have obtained the defence plans for their Hive Worlds. The beetles "
            + "know we've got something but not what. If I transmit the plans to our "
            + "base on Birera they'll intercept the transmission. I need a ship to "
            + "make the run. You're elected. The plans are unipulse coded within "
            + "this transmission. You will be paid. Good luck Commander. ---MESSAGE ENDS.";

    private const string Debrief =
        "You have served us well and we shall remember. "
            + "We did not expect the Thargoids to find out about you."
            + "For the moment please accept this Navy Extra Energy Unit as payment. "
            + "---MESSAGE ENDS.";

    /// <summary>
    /// The stages, in the order the commander passes through them, under the
    /// names the save file has always used.
    /// </summary>
    private static readonly MissionStages s_declared = new([None, Summoned, CarryingPlans, Rewarded]);

    /// <inheritdoc/>
    public string Name => Id;

    /// <inheritdoc/>
    public MissionStages Stages => s_declared;

    /// <inheritdoc/>
    public MissionStep? Advance(IMissionContext context, string stage)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Fires only once: the Constrictor's stage settles at Rewarded for good.
        if (string.Equals(stage, None, StringComparison.Ordinal)
            && string.Equals(context.StageOf(ConstrictorMission.Id), ConstrictorMission.Rewarded, StringComparison.Ordinal)
            && context.CombatScore >= DangerousScore
            && context.GalaxyNumber == RunGalaxy)
        {
            return s_declared.Step(stage, Summoned, new MissionBriefing { Paragraphs = [BriefA] });
        }

        if (string.Equals(stage, Summoned, StringComparison.Ordinal) && IsAt(context, Ceerdi))
        {
            // Blake hands the plans over in person, so this is the one briefing
            // with somebody pictured on it.
            return s_declared.Step(
                stage,
                CarryingPlans,
                new MissionBriefing
                {
                    Paragraphs = [BriefB, BriefC],
                    Portrait = MissionPortrait.Blake,
                });
        }

        return string.Equals(stage, CarryingPlans, StringComparison.Ordinal) && IsAt(context, Birera)
            ? s_declared.Step(
                stage,
                Rewarded,
                new MissionBriefing
                {
                    Headline = "Well done Commander!",
                    Paragraphs = [Debrief],
                },
                new MissionAward(256, 0, MissionEquipment.NavalEnergyUnit))
            : null;
    }

    /// <summary>
    /// The Thargoids that come for the stolen plans, on top of whatever traffic
    /// the system would have had - which is what makes the run back to Birera
    /// the fight it is.
    /// </summary>
    /// <param name="context">The game, seen through the mission facade.</param>
    /// <param name="stage">The stage the commander has reached.</param>
    /// <returns>The ambush to roll for, or null.</returns>
    public AmbushEncounter? Ambush(IMissionContext context, string stage)
        => string.Equals(stage, CarryingPlans, StringComparison.Ordinal)
            ? new AmbushEncounter("Thargoid", AmbushChance)
            : null;

    /// <inheritdoc/>
    public LoneWolfEncounter? LoneWolfSubstitute(IMissionContext context, string stage) => null;

    private static bool IsAt(IMissionContext context, int planetNumber)
        => context.GalaxyNumber == RunGalaxy && context.CurrentPlanetNumber == planetNumber;
}

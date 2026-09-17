// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Missions.Classic;
using EliteSharpLib.Missions;

namespace EliteSharpLib.Tests.Missions;

// Stages are strings, since an enum can't cover a mission in an assembly the game was never built against.
public class MissionProgressTests
{
    [Fact]
    public void StartsEveryMissionAtItsOwnFirstStage()
    {
        // Arrange & Act
        MissionProgress progress = Progress();

        // Assert
        Assert.Equal(ConstrictorMission.None, progress.StageOf(ConstrictorMission.Id));
        Assert.Equal(ThargoidMission.None, progress.StageOf(ThargoidMission.Id));
    }

    [Fact]
    public void RecordsNothingUntilAMissionHasMoved()
    {
        // Arrange & Act: a fresh commander writes no mission entries at all.
        MissionProgress progress = Progress();

        // Assert
        Assert.Empty(progress.Recorded);
    }

    [Fact]
    public void RemembersAStageItWasMovedTo()
    {
        // Arrange
        MissionProgress progress = Progress();

        // Act
        progress.MoveTo(ConstrictorMission.Id, ConstrictorMission.Briefed);

        // Assert
        Assert.Equal(ConstrictorMission.Briefed, progress.StageOf(ConstrictorMission.Id));
        Assert.True(progress.IsAt(ConstrictorMission.Id, ConstrictorMission.Briefed));
        Assert.Equal(ConstrictorMission.Briefed, progress.Recorded[ConstrictorMission.Id]);
    }

    [Fact]
    public void MovesOneMissionWithoutMovingAnother()
    {
        // Arrange: the point of a mission each - two states, not two readings
        // of one number.
        MissionProgress progress = Progress();

        // Act
        progress.MoveTo(ConstrictorMission.Id, ConstrictorMission.Rewarded);

        // Assert
        Assert.Equal(ThargoidMission.None, progress.StageOf(ThargoidMission.Id));
    }

    [Fact]
    public void RefusesAStageTheMissionNeverDeclared()
    {
        // Arrange
        MissionProgress progress = Progress();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => progress.MoveTo(ConstrictorMission.Id, "NoSuchStage"));
    }

    [Fact]
    public void RefusesOneMissionsStageOnAnotherMission()
    {
        // Arrange: 'Summoned' is a real stage, but not this mission's - which
        // the single mission number this replaced could not tell apart.
        MissionProgress progress = Progress();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => progress.MoveTo(ConstrictorMission.Id, ThargoidMission.Summoned));
    }

    [Fact]
    public void RefusesToMoveAMissionThatIsNotInstalled()
    {
        // Arrange
        MissionProgress progress = Progress();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => progress.MoveTo("Generation", "Briefed"));
    }

    [Fact]
    public void ReadsAMissionThatIsNotInstalledAsNoAnswerAtAll()
    {
        // Arrange: null is "there is no such mission", which is not the same
        // answer as "not started" and must not be mistaken for it.
        MissionProgress progress = Progress();

        // Act & Assert
        Assert.Null(progress.StageOf("Generation"));
        Assert.False(progress.IsAt("Generation", "Briefed"));
    }

    [Fact]
    public void ForgetsEverythingWhenCleared()
    {
        // Arrange
        MissionProgress progress = Progress();
        progress.MoveTo(ThargoidMission.Id, ThargoidMission.Summoned);

        // Act
        progress.Clear();

        // Assert
        Assert.Equal(ThargoidMission.None, progress.StageOf(ThargoidMission.Id));
        Assert.Empty(progress.Recorded);
    }

    private static MissionProgress Progress() => new(TestMissions.Registry());
}

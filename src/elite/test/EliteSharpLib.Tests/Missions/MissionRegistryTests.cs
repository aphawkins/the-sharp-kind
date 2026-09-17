// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Missions;
using EliteSharp.Missions.Classic;
using EliteSharpLib.Missions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EliteSharpLib.Tests.Missions;

[Trait("Level", "Integration")]
public class MissionRegistryTests
{
    [Fact]
    public void HoldsNothingWhenNoMissionWasFound()
        => Assert.Empty(Registry().All);

    [Fact]
    public void FindsAMissionByTheNameTheSaveFileRecords()
    {
        // Arrange
        StubMission errand = new("Errand");
        MissionRegistry registry = Registry(errand, new StubMission("Constrictor"));

        // Act & Assert
        Assert.Same(errand, registry.Find("Errand"));
        Assert.Equal(2, registry.All.Count);
    }

    [Fact]
    public void FindsNoMissionWhoseNameNoPluginProvides()
    {
        // Arrange: this is what a save file naming a removed plugin comes back
        // as, and it has to be an answer rather than a crash.
        MissionRegistry registry = Registry(new StubMission("Errand"));

        // Act & Assert
        Assert.Null(registry.Find("Thargoid"));
    }

    [Fact]
    public void TellsMissionNamesApartByTheirBytes()
    {
        // Arrange: a mission name is a save-file key, not prose.
        MissionRegistry registry = Registry(new StubMission("Errand"));

        // Act & Assert
        Assert.Null(registry.Find("errand"));
    }

    [Fact]
    public void RefusesToStartWhenTwoMissionsAnswerToOneName()
    {
        // Arrange: a save file naming it could mean either, so there is nothing
        // safe to do but stop.
        StubMission[] missions = [new("Errand"), new("Errand")];

        // Act & Assert
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Registry(missions));
        Assert.Contains("Errand", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FindsTheShippedMissionsOnDiskThroughTheGamesOwnComposition()
    {
        // The whole seam end to end: found in the Missions folder beside the binary, exactly how a stranger's mission would be found.
        using HeadlessGameHarness harness = new();

        // Act
        MissionRegistry registry = harness.Resolve<MissionRegistry>();

        // Assert
        Assert.Equal(2, registry.All.Count);
        Assert.NotNull(registry.Find(ConstrictorMission.Id));
        Assert.NotNull(registry.Find(ThargoidMission.Id));
    }

    private static MissionRegistry Registry(params IMission[] missions)
        => new(missions, NullLogger<MissionRegistry>.Instance);

    private sealed class StubMission(string name) : IMission
    {
        public string Name { get; } = name;

        public MissionStages Stages { get; } = new(["NotStarted"]);

        public MissionStep? Advance(IMissionContext context, string stage) => null;
    }
}

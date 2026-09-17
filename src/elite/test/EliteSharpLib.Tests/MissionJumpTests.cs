// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Missions.Classic;
using EliteSharpLib.Views;
using SharpKind.Input;

namespace EliteSharpLib.Tests;

// Checks that a jump lands on the right screen with the right briefing - i.e. the entry
// conditions still match what each screen's Reset tests for.
[Trait("Level", "Integration")]
public class MissionJumpTests
{
    // The first two stages belong to the Constrictor mission and the rest to the Thargoid one;
    // Thargoid jumps also want the Constrictor paid for, the one dependency between the two.
    [Theory]
    [InlineData(0, ConstrictorMission.Briefed, ThargoidMission.None)]
    [InlineData(1, ConstrictorMission.Rewarded, ThargoidMission.None)]
    [InlineData(2, ConstrictorMission.Rewarded, ThargoidMission.Summoned)]
    [InlineData(3, ConstrictorMission.Rewarded, ThargoidMission.CarryingPlans)]
    [InlineData(4, ConstrictorMission.Rewarded, ThargoidMission.Rewarded)]
    public void EachStageReachesItsBriefing(int stage, string expectedConstrictor, string expectedThargoid)
    {
        using HeadlessGameHarness harness = new();
        harness.Run(3, [new(1, ConsoleKey.N, KeyScriptAction.Tap), new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap)]);

        MissionJump.To(harness.Game.State, new PlanetController(harness.Game.State), stage);

        Assert.Equal(Screen.MissionBriefing, harness.Game.State.CurrentScreen);
        Assert.Equal(expectedConstrictor, harness.Game.State.Cmdr.Missions.StageOf(ConstrictorMission.Id));
        Assert.Equal(expectedThargoid, harness.Game.State.Cmdr.Missions.StageOf(ThargoidMission.Id));
    }

    [Fact]
    public void CountCoversEveryStage()
    {
        using HeadlessGameHarness harness = new();
        harness.Run(3, [new(1, ConsoleKey.N, KeyScriptAction.Tap), new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap)]);

        // Cycling Count times from any starting point must come back round
        // rather than falling off the end into the default branch.
        for (int stage = 0; stage < MissionJump.Count; stage++)
        {
            MissionJump.To(harness.Game.State, new PlanetController(harness.Game.State), stage);
            Assert.Equal(Screen.MissionBriefing, harness.Game.State.CurrentScreen);
        }
    }

    // Reading M before checking Ctrl consumed the one-shot press, so a bare M stopped firing missiles when the cheat was enabled.
    [Fact]
    public void ABareMIsLeftForTheMissile()
    {
        using EnvironmentVariableScope jump = EnvironmentVariableScope.Set(MissionJump.EnvVar, "1");
        using HeadlessGameHarness harness = new();
        harness.Run(3, [new(1, ConsoleKey.N, KeyScriptAction.Tap), new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap)]);

        // Docked, so nothing else claims M either: whether the press
        // survives the tick is exactly whether the cheat swallowed it.
        Assert.True(harness.Game.State.IsDocked);
        harness.Keyboard.KeyDown(ConsoleKey.M, ConsoleModifiers.None);
        harness.Step([]);

        Assert.True(harness.Keyboard.IsPressed(ConsoleKey.M));
    }

    [Fact]
    public void IsDisabledUnlessTheEnvironmentVariableIsSet()
        => Assert.Equal(
            Environment.GetEnvironmentVariable(MissionJump.EnvVar) is not null,
            MissionJump.IsEnabled);
}

// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Ships;
using SharpKind.Input;

namespace EliteSharpLib.Tests;

// Yaw is not Elite's; lives behind ELITE_DEBUG_YAW. Unset, the keys must do nothing, keeping the shipped game and golden traces unchanged.
[Trait("Level", "Integration")]

// Serialised against other classes touching ELITE_DEBUG_YAW: it's process-wide, and xUnit runs classes in parallel.
[Collection("EnvironmentVariables")]
public class DebugYawTests
{
    // Launch zeroes the yaw, so the key must be held after it.
    private static readonly KeyScriptEvent[] s_launchThenYawLeft =
    [
        new(1, ConsoleKey.N, KeyScriptAction.Tap),
        new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap),
        new(4, ConsoleKey.F1, KeyScriptAction.Tap),
        new(40, ConsoleKey.Q, KeyScriptAction.Hold),
    ];

    [Fact]
    public void TheYawKeysTurnTheShipWhenTheVariableIsSet()
    {
        using EnvironmentVariableScope yaw = EnvironmentVariableScope.Set(DebugYaw.EnvVar, "1");
        using HeadlessGameHarness harness = new(randomSeed: 1);

        harness.Run(60, s_launchThenYawLeft);

        Assert.True(harness.Resolve<PlayerShip>().Yaw < 0);
    }

    [Fact]
    public void TheYawKeysDoNothingWhenTheVariableIsUnset()
    {
        using EnvironmentVariableScope yaw = EnvironmentVariableScope.Set(DebugYaw.EnvVar, null);
        using HeadlessGameHarness harness = new(randomSeed: 1);

        harness.Run(60, s_launchThenYawLeft);

        Assert.Equal(0, harness.Resolve<PlayerShip>().Yaw);
    }

    // The stick centres itself like roll, so a released yaw decays rather than turning forever.
    [Fact]
    public void AReleasedYawLevelsOut()
    {
        using EnvironmentVariableScope yaw = EnvironmentVariableScope.Set(DebugYaw.EnvVar, "1");
        using HeadlessGameHarness harness = new(randomSeed: 1);

        harness.Run(
            120,
            [
                new(1, ConsoleKey.N, KeyScriptAction.Tap),
                new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap),
                new(4, ConsoleKey.F1, KeyScriptAction.Tap),
                new(40, ConsoleKey.Q, KeyScriptAction.Hold),
                new(60, ConsoleKey.Q, KeyScriptAction.Release),
            ]);

        Assert.Equal(0, harness.Resolve<PlayerShip>().Yaw);
    }
}

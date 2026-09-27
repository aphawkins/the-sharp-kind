// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Ships;
using EliteSharpLib.Views;
using SharpKind.Input;

namespace EliteSharpLib.Tests;

// Drives a real death and restart: the game after a restart must not carry
// anything over from the run that died.
[Trait("Level", "Integration")]
public class DeathRestartTests
{
    // Long enough for the wreckage's hundred ticks and the reset after them.
    private const int TicksToRestart = 120;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DyingMidCountdownLeavesNoJumpForTheNextRun(bool galactic)
    {
        using HeadlessGameHarness harness = new(randomSeed: 1);
        harness.Run(3, [new(1, ConsoleKey.N, KeyScriptAction.Tap), new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap)]);
        GameState state = harness.Game.State;
        Space space = harness.Resolve<Space>();

        space.LaunchPlayer();
        state.IsDocked = false;
        state.SetView(Screen.FrontView);
        if (galactic)
        {
            harness.Resolve<PlayerShip>().HasGalacticHyperdrive = true;
            space.StartGalacticHyperspace();
        }
        else
        {
            Assert.True(harness.Resolve<PlanetController>().FindPlanetByName("DISO"));
            space.StartHyperspace();
        }

        harness.Run(1, []);
        Assert.True(space.IsHyperspaceReady);

        state.GameOver();
        harness.Run(TicksToRestart, []);

        Assert.Equal(Screen.IntroOne, state.CurrentScreen);
        Assert.False(space.IsHyperspaceReady);
        Assert.Equal(0, space.HyperCountdown);
        Assert.False(space.HyperGalactic);
        Assert.Empty(space.HyperName);
    }
}

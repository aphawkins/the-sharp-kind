// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Views;
using SharpKind.Input;

namespace EliteSharpLib.Tests;

// As in the original, the hyperspace key works from the charts in flight, not only the cockpit windows.
[Trait("Level", "Integration")]
public class HyperspaceFromChartTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheHyperspaceKeyStartsTheCountdownFromAChartInFlight(bool galacticChart)
    {
        using HeadlessGameHarness harness = StartOnChart(ChartScreen(galacticChart), docked: false);
        int tick = harness.Tick;

        harness.Run(2, [new(tick, ConsoleKey.H, KeyScriptAction.Tap)]);

        Space space = harness.Resolve<Space>();
        Assert.True(space.IsHyperspaceReady);
        Assert.Equal(ChartScreen(galacticChart), harness.Game.State.CurrentScreen);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheHyperspaceKeyDoesNothingFromAChartWhenDocked(bool galacticChart)
    {
        using HeadlessGameHarness harness = StartOnChart(ChartScreen(galacticChart), docked: true);
        int tick = harness.Tick;

        harness.Run(2, [new(tick, ConsoleKey.H, KeyScriptAction.Tap)]);

        Assert.False(harness.Resolve<Space>().IsHyperspaceReady);
    }

    // Screen is internal, so a public theory cannot take it as data.
    private static Screen ChartScreen(bool galacticChart)
        => galacticChart ? Screen.GalacticChart : Screen.ShortRangeChart;

    private static HeadlessGameHarness StartOnChart(Screen chart, bool docked)
    {
        HeadlessGameHarness harness = new(randomSeed: 1);
        harness.Run(3, [new(1, ConsoleKey.N, KeyScriptAction.Tap), new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap)]);
        GameState state = harness.Game.State;

        Assert.True(harness.Resolve<PlanetController>().FindPlanetByName("DISO"));
        if (!docked)
        {
            harness.Resolve<Space>().LaunchPlayer();
            state.IsDocked = false;
        }

        state.SetView(chart);
        harness.Run(1, []);

        return harness;
    }
}

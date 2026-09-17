// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Input;

namespace EliteSharpLib.Tests;

/// <summary>
/// The "Docking Computers On" reminder, which moved onto the housekeeping
/// clock when the MCount jobs were converted.
/// </summary>
/// <remarks>
/// It gets a test of its own because nothing else reaches it: no golden
/// trace flies with the autopilot engaged, since Commander Jameson has no
/// docking computer to engage. Moving it was the point - checked beside the
/// autopilot it would fire on every update the count happened to be sitting
/// on a multiple of 128, which at 13.5Hz is once but at any faster rate is
/// several times over.
/// </remarks>
[Trait("Level", "Integration")]
public class DockingComputerReminderTests
{
    [Fact]
    public void TheReminderFiresOnceEveryHundredAndTwentyEightCounts()
    {
        using HeadlessGameHarness harness = new(randomSeed: 1);

        // Into flight: N past the title, Space past the parade, F1 to launch.
        KeyScriptEvent[] script =
        [
            new(1, ConsoleKey.N, KeyScriptAction.Tap),
            new(2, ConsoleKey.Spacebar, KeyScriptAction.Tap),
            new(4, ConsoleKey.F1, KeyScriptAction.Tap),
        ];
        harness.Run(40, script);

        harness.Resolve<Pilot>().EngageAutoPilot();
        Assert.True(harness.Game.State.MCount is >= 0 and <= 255);

        // 256 counts is one full turn of the counter; passes a multiple of 128 exactly twice.
        int reminders = CountReminders(harness, 256);

        Assert.Equal(2, reminders);
    }

    // Counts fresh reminders, not ticks on screen: InfoMessage resets the dwell counter to 37, so a rising edge is a new message.
    private static int CountReminders(HeadlessGameHarness harness, int ticks)
    {
        const string Reminder = "Docking Computers On";

        int count = 0;
        float previousDwell = harness.Game.State.MessageCount;

        for (int tick = 0; tick < ticks; tick++)
        {
            harness.Run(1, []);

            float dwell = harness.Game.State.MessageCount;
            if (harness.Game.State.MessageString == Reminder && dwell > previousDwell)
            {
                count++;
            }

            previousDwell = dwell;
        }

        return count;
    }
}

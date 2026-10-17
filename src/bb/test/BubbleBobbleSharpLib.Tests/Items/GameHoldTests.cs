// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// The effects that wait in themselves, in the game loop: the pass that took the item stops, the waits run two frames a pass
// with the IRQ's work, and the pass goes on once the routine is over.
public sealed class GameHoldTests
{
    private const byte Idle = 0xFF;
    private const int FirstEnemy = 2;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Asset("zones.json"));

    // $2EEA: six flashes three frames apart, yellow EOR 2 first, with nothing moving; then the giant, and at last the slots
    // handed back.
    [Fact]
    public void TheGiantFlashesForNinePassesThenShowsAndIsHandedBack()
    {
        GameLoop loop = Start();

        loop.World.Apply(29, 0);
        loop.Pass([Idle, Idle]);

        List<int?> borders = [];

        while (loop.Border is not null || borders.Count == 0)
        {
            loop.Pass([Idle, Idle]);
            borders.Add(loop.Border);

            if (loop.Border is not null)
            {
                Assert.All(loop.Entities.X[FirstEnemy..].ToArray(), x => Assert.Equal(0, x));
            }
        }

        Assert.Equal([5, 7, 7, 5, 7, 7, 5, 7, 7, null], borders);
        Assert.All(loop.Entities.State[FirstEnemy..].ToArray(), state => Assert.Equal(SpriteShow.GiantState, state));
        Assert.Contains(loop.Entities.Slot(FirstEnemy).X, (byte[])[0x78, 0x54]);
        Assert.True(loop.Show.On);

        for (int pass = 0; loop.Show.On && pass < 500; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        Assert.False(loop.Show.On);
        Assert.Equal(0, loop.Entities.SpriteExpand);
        Assert.All(loop.Show.Pictures.ToArray(), b => Assert.Equal(0, b));
    }

    // $2E15: a row a pass from the bottom while the IRQ moves the enemies on, and only then are they killed.
    [Fact]
    public void TheWipeGoesUpARowAPassWhileTheEnemiesMoveAndThenKillsThem()
    {
        GameLoop loop = Start();
        byte[] before = loop.Entities.X.ToArray();
        bool moved = false;

        loop.World.Apply(8, 0);
        loop.Pass([Idle, Idle]);

        for (int row = 1; row <= ScreenWipe.Rows; row++)
        {
            loop.Pass([Idle, Idle]);

            Assert.Equal(row, loop.WipedRows);
            Assert.Null(loop.Border);
            moved |= !loop.Entities.X.ToArray().AsSpan(FirstEnemy).SequenceEqual(before.AsSpan(FirstEnemy));
            Assert.DoesNotContain(loop.Entities.State[FirstEnemy..].ToArray(), state => state == 0x0B);
        }

        // The last row is up, and the next pass, with no more frames of the wait, goes on from the kill.
        Assert.False(loop.Wiping);
        loop.Pass([Idle, Idle]);

        Assert.True(moved);
        Assert.Equal(ScreenWipe.Rows, loop.WipedRows);
        Assert.All(
            loop.Entities.State[FirstEnemy..].ToArray(),
            state => Assert.True(state is 0 or >= 0x0B, $"state {state:X2}"));
    }

    // $1AC0: a sweep's start flashes the border yellow, then black, four times five frames, after the slots' wait.
    [Fact]
    public void TheGiantBubblesStartFlashesYellow()
    {
        GameLoop loop = Start();
        loop.Giant.Sweeps = 2;

        List<int?> borders = [];

        for (int pass = 0; pass < 40; pass++)
        {
            loop.Pass([Idle, Idle]);
            borders.Add(loop.Border);
        }

        Assert.Contains(7, borders);
        Assert.DoesNotContain(borders, b => b is not (null or 7));
    }

    // $1621: the Barons' call waits, paused, for the slots to pop twice, a frame at a time, and only then do the Barons come.
    [Fact]
    public void TheBaronsWaitFrameByFrameWithNothingMoving()
    {
        GameLoop loop = Start();
        loop.Timer.Seconds = 0;
        loop.Timer.Hurry = 1;
        byte[] before = loop.Entities.X.ToArray();
        int passes = 0;

        loop.Pass([Idle, Idle]);

        while (loop.Objects.Slot(0).Type != 0x2E && passes < 100)
        {
            Assert.Equal(1, loop.Timer.Hurry);
            Assert.Equal(Convert.ToHexString(before), Convert.ToHexString(loop.Entities.X.ToArray()));
            loop.Pass([Idle, Idle]);
            passes++;
        }

        Assert.InRange(passes, 1, 99);
        Assert.Equal(0, loop.Timer.Hurry);
        Assert.Equal(LevelTimer.Stopped, loop.Timer.Frames);
    }

    // $1685: once the slots are free the call waits on, paused, for the round start's tune to end, 107 frames from its ask on
    // the call's pass; then the level's tune is asked for, and its start takes a frame.
    [Fact]
    public void TheBaronsWaitForTheRoundStartsTuneToEnd()
    {
        GameLoop loop = Start();
        loop.Timer.Seconds = 0;
        loop.Timer.Hurry = 1;
        int passes = 0;

        loop.Pass([Idle, Idle]);

        while (loop.Objects.Slot(0).Type != 0x2E && passes < 100)
        {
            loop.Pass([Idle, Idle]);
            passes++;
        }

        // 108 frames, two a pass, and the pass that finds the wait over brings the Barons.
        Assert.Equal((108 / 2) + 1, passes);
    }

    private static GameLoop Start()
    {
        GameLoop loop = new(new(new RandomSource(new Random(1))), 1);
        loop.Start(s_levels.Level(1), s_zones.Zones(1), 1);

        for (int pass = 0; pass < 60; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        return loop;
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The ending's steps were read from the game's `$A5B7` and timed against the real game (the pay, the wait, the blank screen at
// frame 156, the walk); there is no oracle for the waits, so these hold the port to those readings.
public sealed class GameEndingTests
{
    private const byte Idle = 0xFF;
    private const byte Fire = 0xEF;

    private static readonly string s_assets = Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets");

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(s_assets, "Levels", "levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(s_assets, "Levels", "zones.json"));

    [Fact]
    public void ClearingTheLastLevelStartsTheEnding()
    {
        GameLoop loop = Won(1);

        Assert.Null(loop.EndLevel());
        Assert.True(loop.Ending.Active);
        Assert.False(loop.Ending.Blank);
    }

    [Fact]
    public void ThePlayerWhoWonIsPaidAMillion()
    {
        GameLoop loop = Won(1);
        loop.EndLevel();

        Step(loop, 50);

        Assert.Equal([0x10, 0x00, 0x00], loop.Scores.Bytes[3..].ToArray());
        Assert.Equal([0x00, 0x00, 0x00], loop.Scores.Bytes[..3].ToArray());
        Assert.False(loop.Ending.Blank);
    }

    [Fact]
    public void TheScreenGoesBlankForTheTextAndTheGiantWalks()
    {
        GameLoop loop = Won(0);
        loop.EndLevel();

        Step(loop, 50 + 25);

        Assert.True(loop.Ending.Blank);
        Assert.Equal(3, loop.Ending.Lines.Count);
        Assert.Contains(Song.Ending, Songs(loop));
        Assert.Equal(0xFC, loop.Entities.SpriteEnable);
        Assert.True(loop.Boss.Active);
        Assert.Equal(0x8D, loop.Boss.X);
        Assert.Equal(0xB3, loop.Boss.Y);

        Step(loop, 1);

        Assert.Equal(0x8B, loop.Boss.X);
    }

    [Fact]
    public void TheGiantTurnsAtBothEnds()
    {
        GameLoop loop = Won(0);
        loop.EndLevel();
        Step(loop, 50 + 25);

        Step(loop, (0x8D - 0x23) / 2);

        Assert.Equal(0x23, loop.Boss.X);
        Assert.Equal(1, loop.Boss.Direction & 1);
        Assert.Equal(0x18, loop.Boss.Frame);

        // It goes on to the edge, and turns when it has gone past it (`$0854`-`$0858`), the second pixel of that pass being back.
        Step(loop, (0xFF - 0x23) / 2);
        Assert.Equal(0xFF, loop.Boss.X);
        Assert.Equal(1, loop.Boss.Direction & 1);

        Step(loop, 1);

        Assert.Equal(0xFE, loop.Boss.X);
        Assert.Equal(0, loop.Boss.Direction & 1);
        Assert.Equal(0, loop.Boss.Frame);
    }

    [Fact]
    public void TheGameEndsWhenAButtonIsPressedAndLetGo()
    {
        GameLoop loop = Won(0);
        loop.EndLevel();
        Step(loop, 50 + 25);

        loop.Ending.Step([Idle, Idle]);
        Assert.False(loop.Ending.Finished);

        loop.Ending.Step([Idle, Fire]);
        Assert.False(loop.Ending.Finished);

        loop.Ending.Step([Idle, Idle]);
        Assert.True(loop.Ending.Finished);
    }

    private static GameLoop Won(int winner)
    {
        GameLoop loop = new(new(new RandomSource(new Random(1))), 2);
        loop.Start(s_levels.Level(100), s_zones.Zones(100), 100);

        for (int pass = 0; pass < 1000 && !loop.Boss.Active; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        loop.Boss.Frame = BossFight.StunnedFrame;
        loop.Entities.State[winner] = PlayerFrame.PlayingState;
        loop.Entities.X[winner] = loop.Boss.X;
        loop.Entities.Y[winner] = loop.Boss.Y;
        loop.Boss.Move();

        return loop;
    }

    private static List<Song> Songs(GameLoop loop)
    {
        List<Song> songs = [];

        while (loop.TakeSong() is Song song)
        {
            songs.Add(song);
        }

        return songs;
    }

    private static void Step(GameLoop loop, int passes)
    {
        for (int pass = 0; pass < passes; pass++)
        {
            loop.Ending.Step([Idle, Idle]);
        }
    }
}

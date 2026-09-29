// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The special item of type 7's giant bubble in the game loop: the sweeps the effect orders, one at a time, each down the level.
public sealed class GiantBubbleTests
{
    private const byte Idle = 0xFF;

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "zones.json"));

    [Fact]
    public void TheWorldTakesTheEffect() => Assert.True(WorldEffects.Handles(7));

    [Fact]
    public void OrdersTwoToFiveSweeps()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            GameLoop loop = Start(seed);

            loop.Giant.Order();

            Assert.InRange(loop.Giant.Sweeps, 2, 5);
        }
    }

    // Each sweep starts on a pass, puts the giant in the top right, moves it a cell left and a cell down a pass, and is over
    // at row $16; the sweeps count down to none.
    [Fact]
    public void SweepsTheLevelOnceForEachSweepOrdered()
    {
        GameLoop loop = Start(1);
        loop.Giant.Sweeps = 2;

        int starts = 0;
        int shown = 0;
        int lastColumn = int.MaxValue;
        int lastRow = 0;

        for (int pass = 0; pass < 400 && (loop.Giant.Sweeps != 0 || loop.Giant.Phase != 0); pass++)
        {
            loop.Pass([Idle, Idle]);

            if (!loop.Giant.Shown)
            {
                continue;
            }

            shown++;

            // A sweep starts in row 2, and the one before it may have ended in the same pass.
            if (loop.Giant.Row == 2)
            {
                starts++;
            }
            else
            {
                Assert.True(loop.Giant.Column < lastColumn && loop.Giant.Row > lastRow);
            }

            Assert.InRange(loop.Giant.Column, 0, 0x1D);
            lastColumn = loop.Giant.Column;
            lastRow = loop.Giant.Row;
        }

        Assert.Equal(2, starts);
        Assert.Equal(0, loop.Giant.Sweeps);
        Assert.Equal(0, loop.Giant.Phase);
        Assert.True(shown >= 30, $"{shown} passes with the giant in view");
    }

    [Fact]
    public void ALevelStartPutsTheSweepsAway()
    {
        GameLoop loop = Start(1);

        loop.Giant.Sweeps = 3;
        loop.Giant.Phase = 4;
        loop.Start(s_levels.Level(2), s_zones.Zones(2), 2, next: true);

        Assert.Equal(0, loop.Giant.Sweeps);
        Assert.Equal(0, loop.Giant.Phase);
    }

    private static GameLoop Start(int seed)
    {
        GameLoop loop = new(new(new RandomSource(new Random(seed))), 1);
        loop.Start(s_levels.Level(1), s_zones.Zones(1), 1);
        return loop;
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The bonus round ($34A0) through the game loop: what it does to the level, and what a level's start does to it. The
// routine itself is proved against the game in BonusRoundOracleTests.
public sealed class BonusRoundTests
{
    private const byte Idle = 0xFF;

    private static readonly string s_assets = Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets");

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(s_assets, "Levels", "levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(s_assets, "Levels", "zones.json"));

    [Fact]
    public void TheEnemiesTumbleAndThePlayersFlash()
    {
        GameLoop loop = Start(1);

        for (int pass = 0; pass < 40; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        loop.Bonus.Start();

        Assert.Equal(BonusRound.PlayerState, loop.Entities.State[0]);
        Assert.Equal(0, loop.Entities.State[1]);
        Assert.Equal(BonusRound.PlayerState, loop.Round.Respawn);
        Assert.Equal(BonusRound.TumbleState, loop.Round.Release);
        Assert.Contains(Enumerable.Range(2, 6), slot => loop.Entities.State[slot] == BonusRound.TumbleState);
    }

    [Fact]
    public void TheRoundPlaysItsTune()
    {
        GameLoop loop = Start(1);
        _ = loop.TakeSong();

        loop.Bonus.Start();
        loop.Pass([Idle, Idle]);

        Assert.Equal(Song.BonusRound, loop.TakeSong());
    }

    [Fact]
    public void ALevelsStartEndsTheRound()
    {
        GameLoop loop = Start(1);
        loop.Bonus.Start();

        loop.Start(s_levels.Level(2), s_zones.Zones(2), 2, next: true);

        Assert.Equal(RoundState.RestState, loop.Round.Respawn);
        Assert.Equal(RoundState.ReleasedState, loop.Round.Release);
    }

    [Fact]
    public void TheEnemiesSettleOnAPlatformAndWait()
    {
        GameLoop loop = Start(1);

        for (int pass = 0; pass < 40; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        loop.Bonus.Start();

        for (int pass = 0; pass < 200; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        Assert.Contains(Enumerable.Range(2, 6), slot => loop.Entities.State[slot] == 0x17);
        Assert.Equal(BonusRound.PlayerState, loop.Entities.State[0]);
    }

    private static GameLoop Start(int number)
    {
        GameLoop loop = new(new(new RandomSource(new Random(number))), 1);
        loop.Start(s_levels.Level(number), s_zones.Zones(number), number);
        return loop;
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The giant bonus through the game loop: a level cleared with a special item's bonus due drops it, and a player who
// touches it scores what the item said. The routines themselves are proved against the game in SuperBonusOracleTests.
public sealed class SuperBonusTests
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
    public void AClearedLevelWithABonusDueDropsTheBlock()
    {
        GameLoop loop = ClearedWithABonus(0x10, 0);

        Assert.True(loop.Clear.SuperBonus!.Showing);
        Assert.Equal(0x07, loop.Clear.BonusColour);
    }

    [Fact]
    public void AClearedLevelWithNoBonusDueDropsNothing()
    {
        GameLoop loop = Start();

        Clear(loop);

        Assert.False(loop.Clear.SuperBonus!.Showing);
    }

    [Fact]
    public void TheBlockFallsToItsRestingPlace()
    {
        GameLoop loop = ClearedWithABonus(0x10, 0);

        for (int pass = 0; pass < 60; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        Assert.Equal(0xC8, loop.Entities.Slot(2).Y);
        Assert.Equal(0x80, loop.Entities.Slot(2).X);
        Assert.Equal(0x98, loop.Entities.Slot(3).X);
        Assert.Equal(0xC8 + 0x15, loop.Entities.Slot(4).Y);
    }

    [Fact]
    public void APlayerTouchingItScoresAndTakesItAway()
    {
        GameLoop loop = ClearedWithABonus(0x30, 0);

        for (int pass = 0; pass < 60; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        byte[] before = [.. loop.Scores.Bytes];
        loop.Entities.Slot(0).X = 0x90;
        loop.Entities.Slot(0).Y = 0xD0;

        loop.Pass([Idle, Idle]);

        Assert.Equal(0, loop.Entities.Slot(2).State);
        Assert.False(loop.Clear.SuperBonus!.Showing);
        Assert.Equal(before[1] + 0x30, loop.Scores.Bytes[1]);
    }

    private static GameLoop ClearedWithABonus(byte arg, byte stage)
    {
        GameLoop loop = Start();

        loop.Clear.Schedule(0x1B, arg, stage);
        Clear(loop);

        return loop;
    }

    private static GameLoop Start()
    {
        GameLoop loop = new(new(new RandomSource(new Random(1))), 1);
        loop.Start(s_levels.Level(1), s_zones.Zones(1), 1);
        return loop;
    }

    // The level's enemies are gone, and the game finds it clear on its next passes.
    private static void Clear(GameLoop loop)
    {
        for (int slot = 2; slot < 8; slot++)
        {
            loop.Entities.Slot(slot).State = 0;
        }

        loop.Entities.EnemyCount = 0;

        for (int pass = 0; pass < 12; pass++)
        {
            loop.Pass([Idle, Idle]);
        }
    }
}

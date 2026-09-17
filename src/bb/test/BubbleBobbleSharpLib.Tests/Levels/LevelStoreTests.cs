// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

[Trait("Level", "Integration")]
public sealed class LevelStoreTests
{
    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    [Fact]
    public void ReadsAllOneHundredLevels()
    {
        Assert.Equal(100, LevelStore.Count);

        for (int number = 1; number <= LevelStore.Count; number++)
        {
            Assert.Equal(number, s_levels.Level(number).Number);
        }
    }

    // The row the plan names, and the reason this test exists: it proves the export kept the playfield the
    // right way up and the right way round, which a count alone would not.
    [Fact]
    public void KeepsLevelOnesPlatforms()
        => Assert.Equal("..##...##################...##..", s_levels.Level(1).Bitmap[8]);

    [Fact]
    public void EveryLevelIsThirtyTwoByTwentyThree()
    {
        for (int number = 1; number <= LevelStore.Count; number++)
        {
            Level level = s_levels.Level(number);

            Assert.Equal(23, level.Bitmap.Count);
            Assert.All(level.Bitmap, x => Assert.Equal(32, x.Length));
            Assert.All(level.Bitmap, x => Assert.True(x.All(c => c is '#' or '.')));
        }
    }

    [Fact]
    public void ReadsLevelOnesProperties()
    {
        Level level = s_levels.Level(1);

        Assert.Equal(0x21, level.Colours);
        Assert.Equal(0x7F, level.Sidebar);
        Assert.Equal(2, level.BubbleCurrent);
        Assert.Equal(0, level.WrapOpenings);
        Assert.Equal(9, level.FoodDrop.X);
        Assert.Equal(7, level.FoodDrop.Y);
        Assert.Equal(21, level.PowerupSpawn.X);
        Assert.Equal(7, level.PowerupSpawn.Y);
        Assert.Equal(3, level.Enemies.Count);
        Assert.All(level.Enemies, x => Assert.True(x.FaceLeft && x.MoveLeft));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void RefusesALevelNumberTheGameNeverCounts(int number)
        => Assert.Throws<SharpKindException>(() => s_levels.Level(number));
}

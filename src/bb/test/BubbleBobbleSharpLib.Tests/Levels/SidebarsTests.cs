// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Levels;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The sidebar index is the one level byte with a value that means "none", and it is spelled as a
// number well past the designs that exist rather than as a flag, so the range test is what matters.
[Trait("Level", "Integration")]
public sealed class SidebarsTests
{
    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    [Fact]
    public void GivesEveryLevelEitherADesignOrNone()
    {
        for (int number = 1; number <= LevelStore.Count; number++)
        {
            Level level = s_levels.Level(number);
            SidebarModel model = Sidebars.Select(level);

            Assert.True(
                !model.HasDesign || (model.Design >= 0 && model.Design < SidebarModel.DesignCount),
                $"Level {level.Number} asks for design {model.Design}.");
        }
    }

    // Forty-one of the hundred carry $7F, which is past the hundred the reference tests against and so
    // takes the branch that repeats the level's own header tile instead.
    [Fact]
    public void SendsTheLevelsWithoutADesignToTheirHeaderTile()
    {
        int without = Enumerable.Range(1, LevelStore.Count)
            .Count(x => !Sidebars.Select(s_levels.Level(x)).HasDesign);

        Assert.Equal(41, without);
        Assert.False(Sidebars.Select(s_levels.Level(1)).HasDesign);
        Assert.True(Sidebars.Select(s_levels.Level(2)).HasDesign);
    }

    // The designs are used in order from level two onwards, which is the cheapest check that the
    // index survived the export unshifted.
    [Fact]
    public void ReadsTheDesignStraightOffTheLevel()
    {
        Assert.Equal(0, Sidebars.Select(s_levels.Level(2)).Design);
        Assert.Equal(1, Sidebars.Select(s_levels.Level(3)).Design);
        Assert.Equal(2, Sidebars.Select(s_levels.Level(5)).Design);
    }

    // The reference indexes its tables by a level counted from zero, and the store counts from one.
    [Fact]
    public void CountsTheHeaderTileFromZero()
    {
        Assert.Equal(0, Sidebars.Select(s_levels.Level(1)).HeaderTile);
        Assert.Equal(99, Sidebars.Select(s_levels.Level(100)).HeaderTile);
    }
}

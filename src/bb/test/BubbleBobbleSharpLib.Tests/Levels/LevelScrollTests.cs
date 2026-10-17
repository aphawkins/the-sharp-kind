// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

public sealed class LevelScrollTests
{
    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    // $37C9: 3 rows of the old level's wall, 4 of the new level's, and its 25, a row a frame and two a pass.
    [Fact]
    public void TakesSixteenPassesToBringTheNextLevelIn()
    {
        LevelScroll scroll = Scroll();

        for (int pass = 0; pass < 16; pass++)
        {
            Assert.False(scroll.Finished);
            scroll.Pass();
        }

        Assert.True(scroll.Finished);
        Assert.Equal(2, scroll.Next);
        Assert.Equal(0, scroll.IncomingField.Shift);
        Assert.Equal(0, scroll.Split);
    }

    // $20: the new level's colours start where its wall does, under the screen for the first three rows.
    [Theory]
    [InlineData(0, 25)]
    [InlineData(2, 25)]
    [InlineData(4, 24)]
    [InlineData(20, 8)]
    [InlineData(28, 0)]
    public void SplitsTheScreenWhereTheNewLevelStarts(int rows, int split)
    {
        LevelScroll scroll = Scroll();

        for (int row = 0; row < rows; row += 2)
        {
            scroll.Pass();
        }

        Assert.Equal(split, scroll.Split);
        Assert.Equal(split, scroll.OutgoingField.EndRow);
        Assert.Equal(split, scroll.IncomingField.FirstRow);
        Assert.Equal(split, scroll.OutgoingSidebar.EndRow);
        Assert.Equal(split, scroll.IncomingSidebar.FirstRow);
    }

    // The old level's bottom wall goes on under it, and the new level's top wall over it.
    [Fact]
    public void CarriesTheLevelsWallsOnPastTheirEdges()
    {
        LevelScroll scroll = Scroll();

        for (int pass = 0; pass < 2; pass++)
        {
            scroll.Pass();
        }

        Assert.Equal(PlayfieldModel.Rows - 1, scroll.OutgoingField.SourceRow(PlayfieldModel.Rows - 1));
        Assert.Equal(0, scroll.IncomingField.SourceRow(0));
        Assert.Equal(0, scroll.OutgoingSidebar.FirstRow);
        Assert.Equal(-4, scroll.OutgoingSidebar.Shift);
        Assert.Equal(0, scroll.IncomingSidebar.Number);
    }

    // $E4DF, $0746: only the players in the game are drawn, in dragons in bubbles whose frame turns every eight rows.
    [Fact]
    public void ShowsThePlayersInTheGameInBubbles()
    {
        LevelScroll scroll = Scroll();
        EntityTable entities = new();
        entities.Slot(0).State = 1;
        entities.Slot(2).State = 1;

        for (int pass = 0; pass < 4; pass++)
        {
            scroll.Pass();
        }

        SpriteModel sprites = scroll.Sprites(entities);

        Assert.Equal(1, sprites.TransitFrame);
        Assert.True(sprites.IsDrawn(0));
        Assert.False(sprites.IsDrawn(1));
        Assert.False(sprites.IsDrawn(2));
        Assert.Equal(5, sprites.Colour(0));
    }

    private static LevelScroll Scroll()
        => new(Playfield.Build(s_levels.Level(1)), Sidebars.Select(s_levels.Level(1)), s_levels.Level(2));
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

public sealed class LevelChangeTests
{
    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    private static readonly PlayfieldModel s_field = Playfield.Build(s_levels.Level(1));
    private static readonly SidebarModel s_sidebar = Sidebars.Select(s_levels.Level(1));

    // $7FB2-$7FD1: six flashes ten frames apart, red first: five updates red, five not, three times over.
    [Fact]
    public void FlashesRedThreeTimesBeforeASkip()
    {
        LevelChange change = new(s_levels);
        change.SkipTo(3);

        List<bool> shows = [];

        while (!change.Scrolling)
        {
            shows.Add(change.FlashShows);
            Assert.Null(change.Pass(s_field, s_sidebar, 1, new EntityTable()));
        }

        bool[] flash = [true, true, true, true, true, false, false, false, false, false];

        Assert.Equal([.. flash, .. flash, .. flash], shows);
        Assert.False(change.FlashShows);
    }

    // $7FD4-$7FEE: each level on the way is scrolled in, and then the next from it, until the level skipped to.
    [Fact]
    public void ScrollsThroughEveryLevelOnTheWayToTheOneSkippedTo()
    {
        LevelChange change = new(s_levels);
        change.SkipTo(3);

        int? started = null;
        int scrolls = 0;
        bool wasScrolling = false;

        while (started is null)
        {
            started = change.Pass(s_field, s_sidebar, 1, new EntityTable());
            scrolls += change.Scrolling && (!wasScrolling || change.Rows == 0) ? 1 : 0;
            wasScrolling = change.Scrolling;
        }

        Assert.Equal(3, started);
        Assert.Equal(2, scrolls);

        // $E554: the players glide to the new level's start points after; with none in the game there is nowhere to go.
        Assert.True(change.Gliding);
        Assert.Null(change.Pass(s_field, s_sidebar, 3, new EntityTable()));
        Assert.False(change.Active);
    }

    // $0A6A: a cleared level scrolls straight on, with no flash.
    [Fact]
    public void ScrollsAClearedLevelOnWithoutFlashing()
    {
        LevelChange change = new(s_levels);
        change.ScrollTo(s_field, s_sidebar, 1, 2);

        Assert.True(change.Scrolling);
        Assert.False(change.FlashShows);
        Assert.Equal(PlayfieldModel.Rows, change.Fields(s_field)[0].EndRow);
    }
}

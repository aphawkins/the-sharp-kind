// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// A bonus room through the game loop: what a special item of type 16, 17 or 18 does to the level, and what the room does with the
// players' turns. The routines are proved against the game in ItemRoomOracleTests.
public sealed class ItemRoomTests
{
    private const byte Idle = 0xFF;

    private static readonly string s_assets = Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets");

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(s_assets, "Levels", "levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(s_assets, "Levels", "zones.json"));

    [Theory]
    [InlineData(16, 0x3F, 0x0D)]
    [InlineData(17, 0x41, 0x0C)]
    [InlineData(18, 0x40, 0x0F)]
    public void TheItemBeginsARoomOfItsOwnArt(int type, int art, int colour)
    {
        GameLoop loop = Take(type);

        Assert.True(loop.Room.Active);
        Assert.Equal(ItemRoom.Stage.Filling, loop.Room.Phase);
        Assert.Equal(art, loop.Room.Art);
        Assert.Equal(colour, loop.Room.Colour);
        Assert.NotEmpty(loop.Room.Items);
        Assert.Equal(loop.Room.Items.Count, loop.Room.Remaining);
    }

    [Fact]
    public void TheLevelIsClearedOfWhatWasInIt()
    {
        GameLoop loop = Take(16);

        Assert.All(Enumerable.Range(2, 6), slot => Assert.Equal(0, loop.Entities.State[slot]));
        Assert.Equal(0, loop.Entities.EnemyCount);
        Assert.All(loop.Pop.Drawn.ToArray(), drawn => Assert.Equal(BubblePop.NotDrawn, drawn));
        Assert.Equal(0, loop.Water.Mask);
    }

    [Fact]
    public void TheRoomHoldsThePlayersToTheirTimeAndTheClockRunsOnItsOwn()
    {
        GameLoop loop = Take(16);

        for (int pass = 0; pass < 20; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        Assert.Equal(ItemRoom.Stage.Playing, loop.Room.Phase);
        Assert.InRange(loop.Timer.Seconds, ItemRoom.Seconds - 1, ItemRoom.Seconds);
    }

    [Fact]
    public void ThePlayerWhoTakesEveryItemIsPaidAndTheRoomEnds()
    {
        GameLoop loop = Take(16);

        for (int pass = 0; pass < 20; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        int items = loop.Room.Items.Count;
        byte[] before = [.. loop.Scores.Bytes];

        while (loop.Room.Phase == ItemRoom.Stage.Playing)
        {
            RoomItem next = loop.Room.Items.First(item => item.X != 0 || item.Y != 0);
            loop.Entities.X[0] = next.X;
            loop.Entities.Y[0] = next.Y;
            loop.Pass([Idle, Idle]);
        }

        Assert.Equal(ItemRoom.Stage.Results, loop.Room.Phase);
        Assert.Equal(items, loop.Room.Counts[0]);
        Assert.Contains(loop.Room.Lines, line => line.Text == "PERFECT");
        Assert.Contains(loop.Room.Lines, line => line.Text == "100000");

        // 50 for each item, and 100,000 for being the one that took the most.
        Assert.NotEqual(before, loop.Scores.Bytes.ToArray());

        for (int pass = 0; pass < 150 && loop.Room.Active; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        Assert.False(loop.Room.Active);
        Assert.Equal(0x32, loop.Timer.Seconds);
    }

    [Fact]
    public void ARoomWhoseTimeRunsOutPaysNoBonus()
    {
        GameLoop loop = Take(16);

        for (int pass = 0; pass < 20; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        loop.Timer.Seconds = 0;
        loop.Pass([Idle, Idle]);

        Assert.Equal(ItemRoom.Stage.Results, loop.Room.Phase);
        Assert.DoesNotContain(loop.Room.Lines, line => line.Text == "PERFECT");
    }

    // Player 1 reaches the special item, on level 1 at the first pass, which makes its effect happen.
    private static GameLoop Take(int type)
    {
        GameLoop loop = new(new(new RandomSource(new Random(1))), 1);
        loop.Start(s_levels.Level(1), s_zones.Zones(1), 1);

        loop.Items.Type[1] = (byte)type;
        loop.Items.X[1] = loop.Entities.X[0];
        loop.Items.Y[1] = loop.Entities.Y[0];
        loop.Items.Timer[1] = 0x20;

        loop.Pass([Idle, Idle]);

        return loop;
    }
}

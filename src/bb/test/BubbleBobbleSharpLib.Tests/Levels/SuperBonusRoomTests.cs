// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The super bonus room through the game loop: what the special item of type 32 does to the level, and how the room ends it.
// The routines are proved against the game in SuperBonusRoomOracleTests.
public sealed class SuperBonusRoomTests
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
    public void TheItemBeginsTheRoomWithItsSong()
    {
        GameLoop loop = Take();

        Assert.Equal(SuperBonusRoom.Stage.Building, loop.SuperRoom.Phase);
        Assert.Equal(36, loop.SuperRoom.Items.Count);
        Assert.Equal(0x25, loop.SuperRoom.Remaining);
        Assert.Equal(SuperBonusRoom.Colours, loop.Flow.Colours);
        Assert.Equal(0, loop.Entities.EnemyCount);
        Assert.Contains(Song.SuperBonus, Songs(loop));
    }

    [Fact]
    public void TheGiantStandsOverTheRoomUntilTheNextLevel()
    {
        GameLoop loop = Take();

        Assert.True(loop.Boss.Active);
        Assert.Equal(0x74, loop.Boss.X);
        Assert.Equal(0x35, loop.Boss.Y);
        Assert.Equal(0x06, loop.Boss.Colour);

        loop.Pass([Idle, Idle]);

        Assert.Equal(0x74, loop.Boss.X);
        Assert.Equal(0x35, loop.Boss.Y);

        loop.Start(s_levels.Level(3), s_zones.Zones(3), 1);

        Assert.False(loop.Boss.Active);
    }

    [Fact]
    public void ThePlayersStandOnTheFloor()
    {
        GameLoop loop = Take();

        Assert.Equal(0x2C, loop.Entities.Slot(0).X);
        Assert.Equal(0x9D, loop.Entities.Slot(0).Y);
        Assert.Equal(0, loop.Entities.Slot(1).X);
        Assert.Equal(0, loop.Entities.Slot(1).Y);
    }

    [Fact]
    public void TakingEveryItemAndGoingDownEndsTheLevelOneLevelOn()
    {
        GameLoop loop = Take();

        while (loop.SuperRoom.Phase == SuperBonusRoom.Stage.Building)
        {
            loop.Pass([Idle, Idle]);
        }

        Scores expected = new();

        for (int i = 0; i < 6; i++)
        {
            expected.Add(i, loop.Scores.Bytes[i]);
        }

        for (int i = 0; i < 36; i++)
        {
            expected.Add(1, 0x12);
        }

        while (loop.SuperRoom.Remaining > 1)
        {
            RoomItem next = loop.SuperRoom.Items.First(item => item.X != 0 || item.Y != 0);
            loop.Entities.Slot(0).X = next.X;
            loop.Entities.Slot(0).Y = next.Y;
            loop.Pass([Idle, Idle]);
        }

        // $12 to the hundreds for each of the 36.
        Assert.Equal(36, loop.SuperRoom.Counts[0]);
        Assert.Equal(expected.Bytes[..6].ToArray(), loop.Scores.Bytes[..6].ToArray());
        Assert.False(loop.IsComplete);

        loop.Entities.Slot(0).X = 0x8C;
        loop.Entities.Slot(0).Y = 0xDD;
        _ = Songs(loop);

        for (int pass = 0; pass < 60 && loop.SuperRoom.Active; pass++)
        {
            loop.Entities.Slot(0).X = 0x8C;
            loop.Entities.Slot(0).Y = 0xDD;
            loop.Pass([Idle, Idle]);
        }

        Assert.False(loop.SuperRoom.Active);
        Assert.True(loop.IsComplete);
        Assert.Equal(SuperBonusRoom.Colours, loop.Flow.Colours);
        Assert.Contains(Song.Resume, Songs(loop));
        Assert.Equal(3, loop.EndLevel());
    }

    [Theory]
    [InlineData(14, 0x9D)]
    [InlineData(13, 0xDD)]
    [InlineData(2, 0xDD)]
    public void APlayerAtTheDoorGoesDownOnlyWithFewItemsLeft(int remaining, byte y)
    {
        GameLoop loop = Take();
        SuperBonusRoom room = loop.SuperRoom;
        byte[] xs = [.. room.Items.Select(item => item.X)];
        byte[] ys = [.. room.Items.Select(item => item.Y)];

        room.Restore(xs, ys, remaining, 0, 0);
        loop.Entities.Slot(0).X = 0x8C;
        loop.Entities.Slot(0).Y = 0x9D;

        room.Frame();

        Assert.Equal(y, loop.Entities.Slot(0).Y);
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

    // Player 1 reaches the special item, on level 1 at the first pass, which makes its effect happen.
    private static GameLoop Take()
    {
        GameLoop loop = new(new(new RandomSource(new Random(1))), 1);
        loop.Start(s_levels.Level(1), s_zones.Zones(1), 1);

        loop.Items.Type[1] = SuperBonusRoom.Effect;
        loop.Items.X[1] = loop.Entities.Slot(0).X;
        loop.Items.Y[1] = loop.Entities.Slot(0).Y;
        loop.Items.Timer[1] = 0x20;

        loop.Pass([Idle, Idle]);

        return loop;
    }
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Text.Json;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests;

// The songs the game asks for: $F477's music_start, at the level's start and at the moments the reference plays a tune.
public sealed class GameSongTests
{
    private const byte Idle = 0xFF;
    private const byte AllLetters = 0x3F;

    private static readonly string s_assets = Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets");

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(s_assets, "Levels", "levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(s_assets, "Levels", "zones.json"));

    [Fact]
    public void TheFirstLevelStartsWithTheTheme()
    {
        GameLoop loop = Start(1, next: false);

        Assert.Equal(Song.Theme, loop.TakeSong());
        Assert.Null(loop.TakeSong());
    }

    [Fact]
    public void ALevelThatFollowsAnotherResumesTheTune() => Assert.Equal(Song.Resume, Start(2, next: true).TakeSong());

    // $05DD-$05F0: the theme still playing goes on into the next level; nothing is asked for.
    [Fact]
    public void TheThemeGoesOnIntoTheNextLevel()
    {
        GameLoop loop = Start(1, next: false);
        _ = loop.TakeSong();

        loop.Start(s_levels.Level(2), s_zones.Zones(2), 2, next: true);

        Assert.Null(loop.TakeSong());
    }

    // $05DD-$05F0: a tune past the final theme in the song table, the hurry-up's, is followed by the level's tune again.
    [Fact]
    public void TheHurryUpIsFollowedByTheLevelsTuneAgain()
    {
        GameLoop loop = Start(1, next: false);
        loop.Timer.Seconds = 0;
        loop.Timer.Hurry = 0;
        HurryUp(loop);
        _ = Drain(loop);

        loop.Start(s_levels.Level(2), s_zones.Zones(2), 2, next: true);

        Assert.Equal(Song.Resume, loop.TakeSong());
    }

    // $05DC: level 99's tune is always started, over the theme.
    [Fact]
    public void TheLastLevelsTuneIsStartedOverTheTheme()
    {
        GameLoop loop = Start(1, next: false);
        _ = loop.TakeSong();

        loop.Start(s_levels.Level(100), s_zones.Zones(100), 100, next: true);

        Assert.Equal(Song.FinalTheme, loop.TakeSong());
    }

    [Fact]
    public void TheLastLevelHasItsOwnTune() => Assert.Equal(Song.FinalTheme, Start(100, next: true).TakeSong());

    [Fact]
    public void TheClockRunningOutPlaysTheHurryUpTune()
    {
        GameLoop loop = Start(1, next: false);
        _ = loop.TakeSong();
        loop.Timer.Seconds = 0;
        loop.Timer.Hurry = 0;

        HurryUp(loop);

        Assert.Equal([Song.HurryUp, Song.LevelComplete], Drain(loop));
    }

    // $1621's `ldy #$43`: the Barons come, and song 10 (the round start's) plays.
    [Fact]
    public void TheBaronsComingPlaysTheRoundStartTune()
    {
        GameLoop loop = Start(1, next: false);
        _ = loop.TakeSong();
        loop.Timer.Seconds = 0;
        loop.Timer.Hurry = 1;

        loop.Pass([Idle, Idle]);

        Assert.Contains(Song.RoundStart, Drain(loop));
    }

    // $04A0-$04B8: a death over the level's tune asks for nothing, so the tune plays on.
    [Fact]
    public void ADeathLeavesTheLevelsTunePlaying()
    {
        GameLoop loop = Start(1, next: false);
        _ = loop.TakeSong();
        loop.Entities.Slot(0).State = PlayerFrame.DeadState;

        loop.Pass([Idle, Idle]);

        Assert.DoesNotContain(Song.Resume, Drain(loop));
    }

    // $04A0-$04B8: a death over the hurry-up's tune resumes the level's.
    [Fact]
    public void ADeathAfterTheHurryUpResumesTheLevelsTune()
    {
        GameLoop loop = Start(1, next: false);
        loop.Timer.Seconds = 0;
        loop.Timer.Hurry = 0;
        HurryUp(loop);
        _ = Drain(loop);
        loop.Entities.Slot(0).State = PlayerFrame.DeadState;

        loop.Pass([Idle, Idle]);

        Assert.Contains(Song.Resume, Drain(loop));
    }

    [Fact]
    public void AllSixLettersPlayTheExtendFanfare()
    {
        GameLoop loop = Start(1, next: false);
        _ = loop.TakeSong();
        loop.PlayerTable.ExtendLetters[0] = AllLetters;

        loop.Pass([Idle, Idle]);

        Assert.Contains(Song.Extend, Drain(loop));
    }

    // Every song the game asks for has a place in the rendition's manifest, and names a song of the SID. The HVSC file has the
    // title tune and the EXTEND fanfare the other way round from the reference's table.
    [Fact]
    public void EverySongIsInTheManifest()
    {
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(s_assets, "AssetManifest.json")));
        JsonElement music = manifest.RootElement.GetProperty("Music");

        foreach (Song song in Enum.GetValues<Song>())
        {
            int number = song switch
            {
                Song.Title => (int)Song.Extend,
                Song.Extend => (int)Song.Title,
                _ => (int)song,
            };

            Assert.Equal($"bubble-bobble.sid#{number}", music.GetProperty(song.ToString()).GetString());
        }

        Assert.True(File.Exists(Path.Combine(s_assets, "Music", "bubble-bobble.sid")));
    }

    // $3AB8: the scroll asks for the hurry-up's tune at row 11, and for song 6 once it has ended.
    private static void HurryUp(GameLoop loop)
    {
        for (int pass = 0; loop.Timer.Hurry == 0 && pass < 100; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        Assert.Equal(1, loop.Timer.Hurry);
    }

    private static List<Song> Drain(GameLoop loop)
    {
        List<Song> songs = [];

        while (loop.TakeSong() is Song song)
        {
            songs.Add(song);
        }

        return songs;
    }

    private static GameLoop Start(int number, bool next)
    {
        GameLoop loop = new(new(new RandomSource(new Random(number))), 1);
        loop.Start(s_levels.Level(number), s_zones.Zones(number), number, next);
        return loop;
    }
}

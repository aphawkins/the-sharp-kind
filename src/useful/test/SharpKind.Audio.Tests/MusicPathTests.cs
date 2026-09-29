// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.Tests;

public class MusicPathTests
{
    [Theory]
    [InlineData("music/tune.sid#8", "music/tune.sid", 8)]
    [InlineData("tune.sid#12", "tune.sid", 12)]
    public void ASongSuffixNamesTheSong(string path, string file, int song)
    {
        (string actualFile, int? actualSong) = MusicPath.Parse(path);

        Assert.Equal(file, actualFile);
        Assert.Equal(song, actualSong);
    }

    [Theory]
    [InlineData("tune.mid")]
    [InlineData("tune.sid")]
    [InlineData("tune.sid#")]
    [InlineData("tune.sid#x")]
    [InlineData("#3")]
    public void ThereIsNoSongWithoutAnAllDigitSuffix(string path)
    {
        (string file, int? song) = MusicPath.Parse(path);

        Assert.Equal(path, file);
        Assert.Null(song);
    }

    [Theory]
    [InlineData("tune.sid", true)]
    [InlineData("tune.SID#3", true)]
    [InlineData("tune.mid", false)]
    [InlineData("tune.ogg#2", false)]
    public void OnlyASidFileIsASid(string path, bool sid) => Assert.Equal(sid, MusicPath.IsSid(path));
}

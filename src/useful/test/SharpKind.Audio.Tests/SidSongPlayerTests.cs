// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.Tests;

public sealed class SidSongPlayerTests : IDisposable
{
    private const int SampleRate = 44100;

    private readonly string _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".sid");

    public SidSongPlayerTests() => File.WriteAllBytes(_path, SidFixtures.BuildTune());

    public void Dispose()
    {
        File.Delete(_path);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void PlaysTheSongAndThenIsFinished()
    {
        SidSongPlayer player = new(_path + "#2", repeat: false);
        float[] first = new float[2 * SampleRate / 5];
        float[] later = new float[2 * SampleRate * 4];

        player.Render(first);
        bool soundedEarly = first.Any(sample => Math.Abs(sample) > 0.01f);
        player.Render(later);

        Assert.True(soundedEarly);
        Assert.True(player.Finished);
        Assert.All(later.Skip(later.Length - 100), sample => Assert.True(Math.Abs(sample) < 0.0001f));
    }

    [Fact]
    public void StartsTheSongAgainWhenItRepeats()
    {
        SidSongPlayer player = new(_path, repeat: true);

        float[] played = new float[2 * SampleRate * 6];

        // In the blocks an audio callback asks for, since the end of a song is noticed between them.
        for (int block = 0; block < played.Length; block += 2 * SampleRate / 10)
        {
            player.Render(played.AsSpan(block, 2 * SampleRate / 10));
        }

        // The song sounds, goes quiet for two seconds, and sounds again.
        int bursts = 0;
        bool loud = false;

        for (int i = 0; i < played.Length; i += 2)
        {
            bool now = Math.Abs(played[i]) > 0.01f;

            if (now && !loud && (bursts == 0 || (i / 2) - LastLoud(played, i) > SampleRate))
            {
                bursts++;
            }

            loud = now;
        }

        Assert.False(player.Finished);
        Assert.True(bursts >= 2, $"the song sounded {bursts} time(s) in six seconds");
    }

    private static int LastLoud(float[] samples, int before)
    {
        for (int i = before - 2; i >= 0; i -= 2)
        {
            if (Math.Abs(samples[i]) > 0.01f)
            {
                return i / 2;
            }
        }

        return 0;
    }
}

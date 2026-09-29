// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Fakes.Assets;

namespace SharpKind.Audio.Tests;

public sealed class SoftwareSoundSidTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".sid");

    public SoftwareSoundSidTests() => File.WriteAllBytes(_path, SidFixtures.BuildTune());

    public void Dispose()
    {
        File.Delete(_path);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void PlaysASidSongAsMusic()
    {
        using SoftwareSound sound = new(Locator());
        sound.Play("Theme", repeat: false);

        float[] buffer = new float[2 * SoftwareSound.SampleRate / 10];
        sound.Render(buffer);

        Assert.Contains(buffer, sample => Math.Abs(sample) > 0.01f);
    }

    [Fact]
    public void StopMusicSilencesASidSong()
    {
        using SoftwareSound sound = new(Locator());
        sound.Play("Theme", repeat: false);
        sound.StopMusic();

        float[] buffer = new float[2 * SoftwareSound.SampleRate / 10];
        sound.Render(buffer);

        Assert.All(buffer, sample => Assert.Equal(0f, sample));
    }

    private FakeAssetLocator Locator()
    {
        FakeAssetLocator locator = new();
        locator.MusicPaths["Theme"] = _path + "#1";

        return locator;
    }
}

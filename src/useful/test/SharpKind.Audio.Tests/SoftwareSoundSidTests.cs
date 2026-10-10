// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Fakes.Assets;

namespace SharpKind.Audio.Tests;

public sealed class SoftwareSoundSidTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".sid");
    private readonly string _effectsPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".sidfx");
    private readonly string _wavPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".wav");

    public SoftwareSoundSidTests()
    {
        File.WriteAllBytes(_path, SidFixtures.BuildTune());
        File.WriteAllText(_effectsPath, SidFixtures.EffectFile);
        File.WriteAllBytes(_wavPath, AudioFixtures.BuildTestWavFloat32(frequencyHz: 300, durationSeconds: 1, amplitude: 0.5f));
    }

    public void Dispose()
    {
        File.Delete(_path);
        File.Delete(_effectsPath);
        File.Delete(_wavPath);
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

    [Fact]
    public void PlaysASidEffect()
    {
        using SoftwareSound sound = new(Locator());
        sound.Play("Beep", 1f, 0f, 1.0);

        float[] buffer = new float[2 * SoftwareSound.SampleRate / 10];
        sound.Render(buffer);

        Assert.Contains(buffer, sample => Math.Abs(sample) > 0.01f);
    }

    [Fact]
    public void ASidEffectAfterStopMusicSounds()
    {
        using SoftwareSound sound = new(Locator());
        sound.Play("Theme", repeat: false);
        sound.StopMusic();
        sound.Play("Beep", 1f, 0f, 1.0);

        float[] buffer = new float[2 * SoftwareSound.SampleRate / 10];
        sound.Render(buffer);

        Assert.Contains(buffer, sample => Math.Abs(sample) > 0.01f);
    }

    [Fact]
    public void OtherMusicStopsASidSong()
    {
        using SoftwareSound withSong = new(Locator());
        using SoftwareSound withoutSong = new(Locator());
        withSong.Play("Theme", repeat: false);
        withSong.Play("Wav", repeat: false);
        withoutSong.Play("Wav", repeat: false);

        float[] played = new float[2 * SoftwareSound.SampleRate / 10];
        float[] expected = new float[played.Length];
        withSong.Render(played);
        withoutSong.Render(expected);

        Assert.Equal(expected, played);
    }

    [Fact]
    public void ASidSongStopsOtherMusic()
    {
        using SoftwareSound sound = new(Locator());
        sound.Play("Wav", repeat: true);
        sound.Play("Theme", repeat: false);

        // The fixture song silences the chip after ten frames; by then only the WAV would still sound.
        float[] buffer = new float[2 * SoftwareSound.SampleRate];
        sound.Render(buffer);

        Assert.All(buffer[^1000..], sample => Assert.True(Math.Abs(sample) < 0.001f));
    }

    private FakeAssetLocator Locator()
    {
        FakeAssetLocator locator = new();
        locator.MusicPaths["Theme"] = _path + "#1";
        locator.MusicPaths["Wav"] = _wavPath;
        locator.SfxPaths["Beep"] = _effectsPath;

        return locator;
    }
}

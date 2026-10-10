// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Fakes.Assets;

namespace SharpKind.Audio.Tests;

public sealed class SidSoundTests : IDisposable
{
    private const int SampleRate = 44100;

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "SidSoundTests_" + Guid.NewGuid().ToString("N"));
    private readonly string _tunePath;
    private readonly string _effectsPath;

    public SidSoundTests()
    {
        Directory.CreateDirectory(_directory);
        _tunePath = Path.Combine(_directory, "tune.sid");
        _effectsPath = Path.Combine(_directory, "effects.sidfx");
        File.WriteAllBytes(_tunePath, SidFixtures.BuildTune());
        File.WriteAllText(_effectsPath, SidFixtures.EffectFile);
    }

    [Fact]
    public void AnEffectSounds()
    {
        using SidSound sound = new(Locator());

        sound.PlayEffect("Beep");

        Assert.True(Loud(Render(sound, 0.1)));
    }

    [Fact]
    public void ASongSounds()
    {
        using SidSound sound = new(Locator());

        sound.PlayMusic("Theme", repeat: false);

        Assert.True(Loud(Render(sound, 0.1)));
    }

    [Fact]
    public void AnEffectDuringASongIsNotHeard()
    {
        using SidSound quiet = new(Locator());
        using SidSound busy = new(Locator());
        quiet.PlayMusic("Theme", repeat: false);
        busy.PlayMusic("Theme", repeat: false);

        busy.PlayEffect("Beep");

        Assert.Equal(Render(quiet, 0.5), Render(busy, 0.5));
    }

    [Fact]
    public void AnEffectAfterStopMusicSounds()
    {
        using SidSound sound = new(Locator());
        sound.PlayMusic("Theme", repeat: false);
        Render(sound, 0.05);

        sound.StopMusic();
        sound.PlayEffect("Beep");

        Assert.True(Loud(Render(sound, 0.1)));
    }

    // The fixture song sounds for ten frames, then silences the chip; two seconds of quiet end it.
    [Fact]
    public void EffectsPlayAgainOnceASongThatDoesNotRepeatHasEnded()
    {
        using SidSound sound = new(Locator());
        sound.PlayMusic("Theme", repeat: false);
        for (int i = 0; i < 30; i++)
        {
            Render(sound, 0.1);
        }

        sound.PlayEffect("Beep");

        Assert.True(Loud(Render(sound, 0.1)));
    }

    [Fact]
    public void ItRoutesOnlyItsOwnEntries()
    {
        FakeAssetLocator locator = Locator();
        locator.SfxPaths["Laser"] = "laser.wav";
        locator.MusicPaths["Danube"] = "danube.mid";

        using SidSound sound = new(locator);

        Assert.True(sound.HasAny);
        Assert.True(sound.HandlesEffect("Beep"));
        Assert.False(sound.HandlesEffect("Laser"));
        Assert.True(sound.HandlesMusic("Theme"));
        Assert.False(sound.HandlesMusic("Danube"));
    }

    [Fact]
    public void AnEffectKeyMissingFromItsFileThrowsSharpKindException()
    {
        FakeAssetLocator locator = new();
        locator.SfxPaths["Nope"] = _effectsPath;

        SharpKindException exception = Assert.Throws<SharpKindException>(() => new SidSound(locator));

        Assert.Contains("Nope", exception.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup; the OS temp directory will be reclaimed eventually.
        }
    }

    private static float[] Render(SidSound sound, double seconds)
    {
        float[] buffer = new float[2 * (int)(SampleRate * seconds)];
        sound.Render(buffer);
        return buffer;
    }

    private static bool Loud(float[] buffer) => buffer.Any(sample => Math.Abs(sample) > 0.01f);

    private FakeAssetLocator Locator()
    {
        FakeAssetLocator locator = new();
        locator.MusicPaths["Theme"] = _tunePath + "#1";
        locator.SfxPaths["Beep"] = _effectsPath;
        locator.SfxPaths["Hyperspace"] = _effectsPath;
        return locator;
    }
}

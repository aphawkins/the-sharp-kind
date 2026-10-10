// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;
using SharpKind.Assets;
using SharpKind.Fakes.Assets;

namespace SharpKind.Audio.Tests;

// Exercises SoftwareSound against small, self-authored fixtures (a minimal MIDI file, SoundFont2 and .wav files) rather than any game project's assets.
// AudioAssetFixture is nested rather than top-level (CA1034) because it is xunit's IClassFixture<T>, which must be public alongside the public test class (xUnit1000, CS0051).
#pragma warning disable CA1034, CA1515
public sealed class SoftwareSoundTests(SoftwareSoundTests.AudioAssetFixture fixture)
    : IClassFixture<SoftwareSoundTests.AudioAssetFixture>
{
    [Fact]
    public void PlayWavSfxProducesNonSilentAudio()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.Play("WavBeep", 1f, 0f, 1.0);

        float[] buffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(buffer);

        (float rms, float maxAbs) = Measure(buffer);
        Assert.True(rms > 0.05f, $"RMS was {rms}");
        Assert.True(maxAbs > 0.1f, $"MaxAbs was {maxAbs}");
    }

    [Fact]
    public void PlayMidiSfxProducesNonSilentAudio()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.Play("MidiNote", 1f, 0f, 1.0);

        float[] buffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(buffer);

        (float rms, float maxAbs) = Measure(buffer);
        Assert.True(rms > 0.001f, $"RMS was {rms}");
        Assert.True(maxAbs > 0.005f, $"MaxAbs was {maxAbs}");
    }

    [Fact]
    public void MidiAndWavSfxBothProduceAudioThroughTheSameInstance()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);

        sound.Play("MidiNote", 1f, 0f, 1.0);
        float[] midiBuffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(midiBuffer);

        sound.Play("WavBeep", 1f, 0f, 1.0);
        float[] wavBuffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(wavBuffer);

        (float midiRms, _) = Measure(midiBuffer);
        (float wavRms, _) = Measure(wavBuffer);

        Assert.True(midiRms > 0.001f, $"midi RMS was {midiRms}");
        Assert.True(wavRms > 0.05f, $"wav RMS was {wavRms}");
    }

    [Fact]
    public void PlayMusicMidiProducesNonSilentAudio()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.Play("MidiTheme", repeat: true);

        float[] buffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(buffer);

        (float rms, _) = Measure(buffer);
        Assert.True(rms > 0.001f, $"RMS was {rms}");
    }

    [Fact]
    public void PlayMusicWavProducesNonSilentAudio()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.Play("WavTheme", repeat: true);

        float[] buffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(buffer);

        (float rms, float maxAbs) = Measure(buffer);
        Assert.True(rms > 0.05f, $"RMS was {rms}");
        Assert.True(maxAbs > 0.1f, $"MaxAbs was {maxAbs}");
    }

    [Fact]
    public void RepeatingWavMusicKeepsSoundingAfterItsEnd()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.Play("WavShort", repeat: true);

        float[] buffer = new float[SoftwareSound.SampleRate * SoftwareSound.Channels];
        sound.Render(buffer);

        (float rms, _) = Measure(buffer.AsSpan(buffer.Length / 2));
        Assert.True(rms > 0.05f, $"RMS was {rms}");
    }

    [Fact]
    public void WavMusicWithoutRepeatIsSilentAfterItsEnd()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.Play("WavShort", repeat: false);

        float[] buffer = new float[SoftwareSound.SampleRate * SoftwareSound.Channels];
        sound.Render(buffer);

        (_, float maxAbs) = Measure(buffer.AsSpan(buffer.Length / 2));
        Assert.Equal(0f, maxAbs);
    }

    [Fact]
    public void StopMusicSilencesOutput()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.Play("WavTheme", repeat: true);
        sound.StopMusic();

        float[] buffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(buffer);

        (float rms, float maxAbs) = Measure(buffer);
        Assert.Equal(0f, rms);
        Assert.Equal(0f, maxAbs);
    }

    [Fact]
    public void PlayLoopProducesNonSilentAudioAcrossTheWrapPoint()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.PlayLoop("WavBeep", 1.0);

        // The source sample is ~0.3s; render a full second so the tail of
        // the buffer can only be non-silent if playback wrapped and kept going.
        float[] buffer = new float[SoftwareSound.SampleRate * SoftwareSound.Channels];
        sound.Render(buffer);

        (float tailRms, float tailMaxAbs) = Measure(buffer.AsSpan(SoftwareSound.SampleRate * SoftwareSound.Channels / 2));
        Assert.True(tailRms > 0.05f, $"Tail RMS was {tailRms}");
        Assert.True(tailMaxAbs > 0.1f, $"Tail MaxAbs was {tailMaxAbs}");
    }

    [Fact]
    public void StopLoopSilencesOutput()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.PlayLoop("WavBeep", 1.0);
        sound.StopLoop();

        float[] buffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(buffer);

        (float rms, float maxAbs) = Measure(buffer);
        Assert.Equal(0f, rms);
        Assert.Equal(0f, maxAbs);
    }

    [Fact]
    public void PlayLoopPitchChangesPlaybackRate()
    {
        using SoftwareSound slow = new(fixture.AssetLocator);
        slow.PlayLoop("WavBeep", 1.0);
        float[] slowBuffer = new float[4096 * SoftwareSound.Channels];
        slow.Render(slowBuffer);

        using SoftwareSound fast = new(fixture.AssetLocator);
        fast.PlayLoop("WavBeep", 2.0);
        float[] fastBuffer = new float[4096 * SoftwareSound.Channels];
        fast.Render(fastBuffer);

        int slowCrossings = CountZeroCrossings(slowBuffer);
        int fastCrossings = CountZeroCrossings(fastBuffer);

        Assert.True(
            fastCrossings > slowCrossings * 1.5,
            $"slow crossings={slowCrossings}, fast crossings={fastCrossings}");
    }

    [Fact]
    public void ConstructingWithAnUnsupportedSfxFormatThrowsSharpKindExceptionNamingTheFile()
    {
        FakeAssetLocator locator = new();
        locator.SfxPaths["Vorbis"] = "beep.ogg";

        SharpKindException exception = Assert.Throws<SharpKindException>(() => new SoftwareSound(locator));
        Assert.Contains("beep.ogg", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PlayingMusicInAnUnsupportedFormatThrowsSharpKindExceptionNamingTheFile()
    {
        FakeAssetLocator locator = new();
        locator.MusicPaths["Vorbis"] = "theme.ogg";
        using SoftwareSound sound = new(locator);

        SharpKindException exception = Assert.Throws<SharpKindException>(() => sound.Play("Vorbis", repeat: false));
        Assert.Contains("theme.ogg", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructingWithATruncatedWavFmtChunkThrowsSharpKindException()
    {
        string directory = Path.Combine(Path.GetTempPath(), "SoftwareSoundTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string malformedPath = Path.Combine(directory, "truncated.wav");
            File.WriteAllBytes(malformedPath, BuildTruncatedFmtChunkWav());

            FakeAssetLocator locator = new();
            locator.SfxPaths["Malformed"] = malformedPath;

            SharpKindException exception = Assert.Throws<SharpKindException>(() => new SoftwareSound(locator));
            Assert.Contains("fmt", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void PlayDropsOneShotsBeyondThePoolSizeInsteadOfThrowing()
    {
        using SoftwareSound sixteen = new(fixture.AssetLocator);
        for (int i = 0; i < 16; i++)
        {
            sixteen.Play("WavQuiet", 1f, 0f, 1.0);
        }

        float[] bufferSixteen = new float[2048 * SoftwareSound.Channels];
        sixteen.Render(bufferSixteen);

        using SoftwareSound seventeen = new(fixture.AssetLocator);
        Exception? exception = Record.Exception(() =>
        {
            for (int i = 0; i < 17; i++)
            {
                seventeen.Play("WavQuiet", 1f, 0f, 1.0);
            }
        });
        Assert.Null(exception);

        float[] bufferSeventeen = new float[2048 * SoftwareSound.Channels];
        seventeen.Render(bufferSeventeen);

        // Pins that the 17th Play() is dropped, not queued: output is byte-for-byte identical to only ever having played 16.
        Assert.Equal(bufferSixteen, bufferSeventeen);

        (float rms, _) = Measure(bufferSixteen);
        Assert.True(rms > 0f, $"RMS was {rms}");
    }

    private static (float Rms, float MaxAbs) Measure(in ReadOnlySpan<float> buffer)
    {
        double sumSquares = 0;
        float maxAbs = 0;
        foreach (float sample in buffer)
        {
            sumSquares += (double)sample * sample;
            maxAbs = Math.Max(maxAbs, Math.Abs(sample));
        }

        float rms = buffer.Length == 0 ? 0f : (float)Math.Sqrt(sumSquares / buffer.Length);
        return (rms, maxAbs);
    }

    // A truncated 'fmt ' chunk (4 bytes, missing sampleRate/bitsPerSample) should throw SharpKindException, not slice out of range.
    private static byte[] BuildTruncatedFmtChunkWav()
    {
        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(4 + 8 + 4 + 8 + 4); // "WAVE" + fmt chunk + data chunk
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(4); // chunk size: only formatTag + channels, missing sampleRate/bitsPerSample
            writer.Write((short)3); // format tag: IEEE float
            writer.Write((short)SoftwareSound.Channels);

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(4);
            writer.Write(0f);
        }

        return stream.ToArray();
    }

    private static int CountZeroCrossings(in ReadOnlySpan<float> interleavedBuffer)
    {
        int crossings = 0;
        float previous = interleavedBuffer[0];
        for (int i = SoftwareSound.Channels; i < interleavedBuffer.Length; i += SoftwareSound.Channels)
        {
            float current = interleavedBuffer[i];
            if (Math.Abs(previous) > 0f && Math.Abs(current) > 0f && Math.Sign(current) != Math.Sign(previous))
            {
                crossings++;
            }

            previous = current;
        }

        return crossings;
    }

    // Shared via IClassFixture so the fixture files are written once per test run.
    public sealed class AudioAssetFixture : IDisposable
    {
        private readonly string _directory;

        public AudioAssetFixture()
        {
            _directory = Path.Combine(Path.GetTempPath(), "SoftwareSoundTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);

            string soundFontPath = WriteFile("test.sf2", AudioFixtures.BuildMinimalSoundFont());
            string notePath = WriteFile("note.mid", AudioFixtures.BuildMinimalMidiFile());
            string beepPath = WriteFile(
                "beep.wav",
                AudioFixtures.BuildTestWavFloat32(frequencyHz: 300, durationSeconds: 0.3, amplitude: 0.5f));
            string quietPath = WriteFile(
                "quiet.wav",
                AudioFixtures.BuildTestWavFloat32(frequencyHz: 300, durationSeconds: 0.3, amplitude: 0.02f));
            string themePath = WriteFile(
                "theme.wav",
                AudioFixtures.BuildTestWavFloat32(frequencyHz: 250, durationSeconds: 2.0, amplitude: 0.3f));

            FakeAssetLocator locator = new();
            locator.SoundFontPaths["Default"] = soundFontPath;
            locator.SfxPaths["MidiNote"] = notePath;
            locator.SfxPaths["WavBeep"] = beepPath;
            locator.SfxPaths["WavQuiet"] = quietPath;
            locator.MusicPaths["MidiTheme"] = notePath;
            locator.MusicPaths["WavTheme"] = themePath;
            locator.MusicPaths["WavShort"] = beepPath;

            AssetLocator = locator;
        }

        public IAssetLocator AssetLocator { get; }

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

        private string WriteFile(string fileName, byte[] content)
        {
            string path = Path.Combine(_directory, fileName);
            File.WriteAllBytes(path, content);
            return path;
        }
    }
}

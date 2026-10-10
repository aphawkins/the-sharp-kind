// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.SoundFonts;
using static SharpKind.Audio.Tests.AudioFixtures;

namespace SharpKind.Audio.Tests;

public sealed class SoundFontTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "SoundFontTests_" + Guid.NewGuid().ToString("N"));

    public SoundFontTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void TheMinimalFontHasOnePresetInstrumentAndSample()
    {
        SoundFont font = SoundFont.Read(WriteFile(BuildMinimalSoundFont()));

        Preset preset = Assert.Single(font.Presets);
        Assert.Equal(0, preset.Bank);
        Assert.Equal(0, preset.Patch);
        Assert.Single(preset.Zones);
        Assert.Single(Assert.Single(font.Instruments).Zones);

        SoundFontSample sample = Assert.Single(font.Samples);
        Assert.Equal(new SoundFontSample(0, 44100, 0, 44100, 44100, OriginalPitch: 60, PitchCorrection: 0), sample);
        Assert.Equal(44100 + 46, font.SampleData.Length);
    }

    [Fact]
    public void AMissingChunkThrowsSharpKindException()
    {
        byte[] file = BuildChunk("RIFF", [.. "sfbk"u8, .. BuildListChunk("INFO", [])]);

        SharpKindException exception = Assert.Throws<SharpKindException>(() => SoundFont.Read(WriteFile(file)));

        Assert.Contains("sdta", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InstrumentZonesInheritFromTheGlobalZone()
    {
        byte[] file = BuildSoundFont(
            [(0, 0, [[(GenInstrument, 0)]])],
            [[(GenCoarseTune, 2)], [(GenSampleId, 0)], [(GenCoarseTune, 5), (GenSampleId, 0)]]);

        SoundFont font = SoundFont.Read(WriteFile(file));

        Instrument instrument = Assert.Single(font.Instruments);
        Assert.Equal(2, instrument.Zones.Count);
        Assert.Equal(2, instrument.Zones[0][GeneratorType.CoarseTune]);
        Assert.Equal(5, instrument.Zones[1][GeneratorType.CoarseTune]);
    }

    [Fact]
    public void PresetTuningAddsToInstrumentTuning()
    {
        byte[] file = BuildSoundFont([(0, 0, [[(GenCoarseTune, 3), (GenInstrument, 0)]])], [[(GenCoarseTune, 2), (GenSampleId, 0)]]);

        VoiceParameters voice = Assert.Single(SoundFont.Read(WriteFile(file)).FindVoices(0, 0, key: 60, velocity: 100));

        Assert.Equal(5, voice.CoarseTune);
        Assert.Equal(60, voice.RootKey);
        Assert.Equal(-12000, voice.AttackVolEnv);
        Assert.Equal(100, voice.ScaleTuning);
    }

    [Fact]
    public void AKeyOutsideEveryRangeFindsNoVoice()
    {
        byte[] file = BuildSoundFont([(0, 0, [[(GenInstrument, 0)]])], [[(GenKeyRange, Range(60, 72)), (GenSampleId, 0)]]);
        SoundFont font = SoundFont.Read(WriteFile(file));

        Assert.Empty(font.FindVoices(0, 0, key: 40, velocity: 100));
        Assert.Single(font.FindVoices(0, 0, key: 64, velocity: 100));
    }

    [Theory]
    [InlineData(8, 5, 5)] // missing in bank 8: the same program in bank 0
    [InlineData(8, 9, 1)] // missing everywhere: bank 8's first program
    [InlineData(128, 25, 7)] // a missing drum kit: bank 128's first kit, never a melodic program
    public void AMissingPresetFallsBack(int bank, int program, int expectedCoarseTune)
    {
        byte[] file = BuildSoundFont(
            [
                (8, 0, [[(GenCoarseTune, 1), (GenInstrument, 0)]]),
                (0, 5, [[(GenCoarseTune, 5), (GenInstrument, 0)]]),
                (0, 25, [[(GenCoarseTune, 25), (GenInstrument, 0)]]),
                (128, 0, [[(GenCoarseTune, 7), (GenInstrument, 0)]]),
            ],
            [[(GenSampleId, 0)]]);

        VoiceParameters voice = Assert.Single(SoundFont.Read(WriteFile(file)).FindVoices(bank, program, key: 60, velocity: 100));

        Assert.Equal(expectedCoarseTune, voice.CoarseTune);
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

    private string WriteFile(byte[] content)
    {
        string path = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".sf2");
        File.WriteAllBytes(path, content);
        return path;
    }
}

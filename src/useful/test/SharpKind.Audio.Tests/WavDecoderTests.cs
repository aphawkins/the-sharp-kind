// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Runtime.InteropServices;
using SharpKind.Audio.Wav;

namespace SharpKind.Audio.Tests;

public sealed class WavDecoderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "WavDecoderTests_" + Guid.NewGuid().ToString("N"));

    public WavDecoderTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void MonoPcm16DecodesToStereoWithLeftEqualToRight()
    {
        byte[] wav = AudioFixtures.BuildTestWavPcm16Mono(frequencyHz: 300, durationSeconds: 0.1, amplitude: 0.5f);
        int monoSamples = (int)(0.1 * SoftwareSound.SampleRate);

        float[] samples = WavDecoder.Decode(WriteFile("mono.wav", wav));

        Assert.Equal(monoSamples * 2, samples.Length);
        for (int i = 0; i < samples.Length; i += 2)
        {
            Assert.Equal(samples[i], samples[i + 1]);
        }

        Assert.Contains(samples, s => Math.Abs(s) > 0.4f);
    }

    [Fact]
    public void StereoFloat32RoundTrips()
    {
        byte[] wav = AudioFixtures.BuildTestWavFloat32(frequencyHz: 300, durationSeconds: 0.1, amplitude: 0.5f);

        float[] samples = WavDecoder.Decode(WriteFile("stereo.wav", wav));

        // The fixture's samples are the last bytes of the file, after the 44-byte header.
        float[] expected = MemoryMarshal.Cast<byte, float>(wav.AsSpan(44)).ToArray();
        Assert.Equal(expected, samples);
    }

    [Fact]
    public void A22050HzFileThrowsSharpKindException()
    {
        byte[] wav = AudioFixtures.BuildTestWavPcm16Mono(frequencyHz: 300, durationSeconds: 0.1, amplitude: 0.5f, sampleRate: 22050);
        string path = WriteFile("slow.wav", wav);

        SharpKindException exception = Assert.Throws<SharpKindException>(() => WavDecoder.Decode(path));

        Assert.Contains("22050", exception.Message, StringComparison.Ordinal);
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

    private string WriteFile(string fileName, byte[] content)
    {
        string path = Path.Combine(_directory, fileName);
        File.WriteAllBytes(path, content);
        return path;
    }
}

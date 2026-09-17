// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Runtime.InteropServices;
using System.Text;
using SharpKind.Assets;
using SharpKind.Fakes.Assets;

namespace SharpKind.Audio.Tests;

// Exercises SoftwareSound against small, self-authored fixtures (a minimal MIDI file, SoundFont2 and .ogg files) rather than any game project's assets.
// AudioAssetFixture is nested rather than top-level (CA1034) because it is xunit's IClassFixture<T>, which must be public alongside the public test class (xUnit1000, CS0051).
#pragma warning disable CA1034, CA1515
public sealed class SoftwareSoundTests(SoftwareSoundTests.AudioAssetFixture fixture)
    : IClassFixture<SoftwareSoundTests.AudioAssetFixture>
{
    [Fact]
    public void PlayOggSfxProducesNonSilentAudio()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.Play("OggBeep", 1f, 0f, 1.0);

        float[] buffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(buffer);

        (float rms, float maxAbs) = Measure(buffer);
        Assert.True(rms > 0.05f, $"RMS was {rms}");
        Assert.True(maxAbs > 0.1f, $"MaxAbs was {maxAbs}");
    }

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
    public void MidiAndOggSfxBothProduceAudioThroughTheSameInstance()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);

        sound.Play("MidiNote", 1f, 0f, 1.0);
        float[] midiBuffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(midiBuffer);

        sound.Play("OggBeep", 1f, 0f, 1.0);
        float[] oggBuffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(oggBuffer);

        (float midiRms, _) = Measure(midiBuffer);
        (float oggRms, _) = Measure(oggBuffer);

        Assert.True(midiRms > 0.001f, $"midi RMS was {midiRms}");
        Assert.True(oggRms > 0.05f, $"ogg RMS was {oggRms}");
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
    public void PlayMusicOggProducesNonSilentAudio()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.Play("OggTheme", repeat: true);

        float[] buffer = new float[4096 * SoftwareSound.Channels];
        sound.Render(buffer);

        (float rms, float maxAbs) = Measure(buffer);
        Assert.True(rms > 0.05f, $"RMS was {rms}");
        Assert.True(maxAbs > 0.1f, $"MaxAbs was {maxAbs}");
    }

    [Fact]
    public void StopMusicSilencesOutput()
    {
        using SoftwareSound sound = new(fixture.AssetLocator);
        sound.Play("OggTheme", repeat: true);
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
        sound.PlayLoop("OggBeep", 1.0);

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
        sound.PlayLoop("OggBeep", 1.0);
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
        slow.PlayLoop("OggBeep", 1.0);
        float[] slowBuffer = new float[4096 * SoftwareSound.Channels];
        slow.Render(slowBuffer);

        using SoftwareSound fast = new(fixture.AssetLocator);
        fast.PlayLoop("OggBeep", 2.0);
        float[] fastBuffer = new float[4096 * SoftwareSound.Channels];
        fast.Render(fastBuffer);

        int slowCrossings = CountZeroCrossings(slowBuffer);
        int fastCrossings = CountZeroCrossings(fastBuffer);

        Assert.True(
            fastCrossings > slowCrossings * 1.5,
            $"slow crossings={slowCrossings}, fast crossings={fastCrossings}");
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
            sixteen.Play("OggQuiet", 1f, 0f, 1.0);
        }

        float[] bufferSixteen = new float[2048 * SoftwareSound.Channels];
        sixteen.Render(bufferSixteen);

        using SoftwareSound seventeen = new(fixture.AssetLocator);
        Exception? exception = Record.Exception(() =>
        {
            for (int i = 0; i < 17; i++)
            {
                seventeen.Play("OggQuiet", 1f, 0f, 1.0);
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

    // Shared via IClassFixture so the mildly expensive Vorbis encoding only happens once per test run.
    public sealed class AudioAssetFixture : IDisposable
    {
        private readonly string _directory;

        public AudioAssetFixture()
        {
            _directory = Path.Combine(Path.GetTempPath(), "SoftwareSoundTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);

            string soundFontPath = WriteFile("test.sf2", BuildMinimalSoundFont());
            string notePath = WriteFile("note.mid", BuildMinimalMidiFile());
            string beepPath = WriteFile("beep.ogg", BuildTestOgg(frequencyHz: 300, durationSeconds: 0.3, amplitude: 0.5f));
            string quietPath = WriteFile("quiet.ogg", BuildTestOgg(frequencyHz: 300, durationSeconds: 0.3, amplitude: 0.02f));
            string themePath = WriteFile("theme.ogg", BuildTestOgg(frequencyHz: 250, durationSeconds: 2.0, amplitude: 0.3f));
            string wavBeepPath = WriteFile("beep.wav", BuildTestWavFloat32(frequencyHz: 300, durationSeconds: 0.3, amplitude: 0.5f));

            FakeAssetLocator locator = new();
            locator.SoundFontPaths["Default"] = soundFontPath;
            locator.SfxPaths["MidiNote"] = notePath;
            locator.SfxPaths["OggBeep"] = beepPath;
            locator.SfxPaths["OggQuiet"] = quietPath;
            locator.SfxPaths["WavBeep"] = wavBeepPath;
            locator.MusicPaths["MidiTheme"] = notePath;
            locator.MusicPaths["OggTheme"] = themePath;

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

        // A single note-on/note-off SMF (format 0, one track): key 60 vel 100, off after 240 ticks (0.25s at default 120bpm/480 ticks-per-quarter).
        private static byte[] BuildMinimalMidiFile()
        {
            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true))
            {
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                WriteBigEndianInt32(writer, 6);
                WriteBigEndianInt16(writer, 0); // format
                WriteBigEndianInt16(writer, 1); // track count
                WriteBigEndianInt16(writer, 480); // resolution (ticks per quarter note)

                byte[] track =
                [
                    0x00, 0x90, 0x3C, 0x64, // delta 0, note on ch0 key60 vel100
                    0x81, 0x70, 0x80, 0x3C, 0x40, // delta 240 (varlen), note off key60 vel64
                    0x00, 0xFF, 0x2F, 0x00, // delta 0, end of track
                ];

                writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                WriteBigEndianInt32(writer, track.Length);
                writer.Write(track);
            }

            return stream.ToArray();
        }

        private static void WriteBigEndianInt32(BinaryWriter writer, int value)
        {
            writer.Write((byte)(value >> 24));
            writer.Write((byte)(value >> 16));
            writer.Write((byte)(value >> 8));
            writer.Write((byte)value);
        }

        private static void WriteBigEndianInt16(BinaryWriter writer, short value)
        {
            writer.Write((byte)(value >> 8));
            writer.Write((byte)value);
        }

        // The smallest RIFF/SoundFont2 file MeltySynth's parser will accept: one sample, one instrument with a single zone, one preset (bank 0, patch 0) pointing at it.
        private static byte[] BuildMinimalSoundFont()
        {
            short[] pcm = GenerateSinePcm16(SoftwareSound.SampleRate, frequencyHz: 440, amplitude: 0.5);

            byte[] infoList = BuildListChunk("INFO", BuildInfoContent());

            short[] padded = new short[pcm.Length + 46];
            pcm.CopyTo(padded.AsSpan());
            byte[] sdtaList = BuildListChunk("sdta", BuildChunk("smpl", MemoryMarshal.AsBytes(padded).ToArray()));

            byte[] pdtaContent =
            [
                .. BuildChunk("phdr", BuildPresetHeaders()),
                .. BuildChunk("pbag", BuildZoneBag()),
                .. BuildChunk("pgen", BuildGenerators(type: 41, value: 0)), // Instrument -> instrument 0
                .. BuildChunk("inst", BuildInstrumentHeaders()),
                .. BuildChunk("ibag", BuildZoneBag()),
                .. BuildChunk("igen", BuildGenerators(type: 53, value: 0)), // SampleID -> sample 0
                .. BuildChunk("shdr", BuildSampleHeaders(pcm.Length)),
            ];
            byte[] pdtaList = BuildListChunk("pdta", pdtaContent);

            byte[] body = [.. Encoding.ASCII.GetBytes("sfbk"), .. infoList, .. sdtaList, .. pdtaList];
            return BuildChunk("RIFF", body);
        }

        private static short[] GenerateSinePcm16(int sampleCount, double frequencyHz, double amplitude)
        {
            short[] samples = new short[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                double t = i / (double)SoftwareSound.SampleRate;
                samples[i] = (short)(amplitude * short.MaxValue * Math.Sin(2 * Math.PI * frequencyHz * t));
            }

            return samples;
        }

        private static byte[] BuildInfoContent()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

            writer.Write(Encoding.ASCII.GetBytes("ifil"));
            writer.Write(4);
            writer.Write((short)2);
            writer.Write((short)1);

            byte[] name = Encoding.ASCII.GetBytes("Test");
            writer.Write(Encoding.ASCII.GetBytes("INAM"));
            writer.Write(name.Length);
            writer.Write(name);

            return stream.ToArray();
        }

        private static byte[] BuildPresetHeaders()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

            writer.Write(Ascii20("Test"));
            writer.Write((ushort)0); // patch
            writer.Write((ushort)0); // bank
            writer.Write((ushort)0); // zoneStartIndex
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            writer.Write(Ascii20("EOP"));
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)1); // zoneStartIndex: closes preset 0's single zone
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            return stream.ToArray();
        }

        private static byte[] BuildInstrumentHeaders()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

            writer.Write(Ascii20("Test"));
            writer.Write((ushort)0); // zoneStartIndex

            writer.Write(Ascii20("EOI"));
            writer.Write((ushort)1); // closes instrument 0's single zone

            return stream.ToArray();
        }

        // Shared by pbag and ibag: one real zone (index 0) followed by the
        // required terminator entry recording the total generator count.
        private static byte[] BuildZoneBag()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

            writer.Write((ushort)0); // generatorIndex
            writer.Write((ushort)0); // modulatorIndex (unused)
            writer.Write((ushort)1); // terminator: total real generator count
            writer.Write((ushort)0);

            return stream.ToArray();
        }

        // Shared by pgen and igen: a single real generator followed by the
        // required (discarded) terminator record.
        private static byte[] BuildGenerators(ushort type, ushort value)
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

            writer.Write(type);
            writer.Write(value);
            writer.Write((ushort)0);
            writer.Write((ushort)0);

            return stream.ToArray();
        }

        private static byte[] BuildSampleHeaders(int sampleCount)
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

            writer.Write(Ascii20("Test"));
            writer.Write(0); // start
            writer.Write(sampleCount); // end
            writer.Write(0); // startLoop
            writer.Write(sampleCount); // endLoop
            writer.Write(SoftwareSound.SampleRate);
            writer.Write((byte)60); // originalPitch: matches the note-on key, so no pitch shift
            writer.Write((sbyte)0); // pitchCorrection
            writer.Write((ushort)0); // link
            writer.Write((ushort)1); // sampleType: Mono

            writer.Write(Ascii20("EOS"));
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write((byte)0);
            writer.Write((sbyte)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);

            return stream.ToArray();
        }

        private static byte[] Ascii20(string text)
        {
            byte[] bytes = new byte[20];
            Encoding.ASCII.GetBytes(text).CopyTo(bytes, 0);
            return bytes;
        }

        private static byte[] BuildChunk(string chunkId, byte[] content)
        {
            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true))
            {
                writer.Write(Encoding.ASCII.GetBytes(chunkId));
                writer.Write(content.Length);
                writer.Write(content);
            }

            return stream.ToArray();
        }

        private static byte[] BuildListChunk(string listType, byte[] content)
        {
            byte[] combined = [.. Encoding.ASCII.GetBytes(listType), .. content];
            return BuildChunk("LIST", combined);
        }

        // Matches the format real .wav assets are authored in: format tag 3 (IEEE float), stereo, 32-bit, at the mixer's own sample rate.
        private static byte[] BuildTestWavFloat32(double frequencyHz, double durationSeconds, float amplitude)
        {
            int sampleRate = SoftwareSound.SampleRate;
            int channels = SoftwareSound.Channels;
            int frameCount = (int)(durationSeconds * sampleRate);
            int dataLength = frameCount * channels * sizeof(float);

            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataLength);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)3); // format tag: IEEE float
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * sizeof(float)); // byte rate
                writer.Write((short)(channels * sizeof(float))); // block align
                writer.Write((short)32); // bits per sample

                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataLength);

                for (int i = 0; i < frameCount; i++)
                {
                    float sample = amplitude * MathF.Sin(2f * MathF.PI * (float)frequencyHz * i / sampleRate);
                    for (int ch = 0; ch < channels; ch++)
                    {
                        writer.Write(sample);
                    }
                }
            }

            return stream.ToArray();
        }

        // Adapted from OggVorbisEncoder's own example (OggVorbisEncoder.Example/Encoder.cs).
        private static byte[] BuildTestOgg(double frequencyHz, double durationSeconds, float amplitude)
        {
            int sampleRate = SoftwareSound.SampleRate;
            int channels = SoftwareSound.Channels;
            int sampleCount = (int)(durationSeconds * sampleRate);

            float[][] samples = new float[channels][];
            for (int ch = 0; ch < channels; ch++)
            {
                samples[ch] = new float[sampleCount];
            }

            for (int i = 0; i < sampleCount; i++)
            {
                float sample = amplitude * MathF.Sin(2f * MathF.PI * (float)frequencyHz * i / sampleRate);
                for (int ch = 0; ch < channels; ch++)
                {
                    samples[ch][i] = sample;
                }
            }

            using MemoryStream outputData = new();
            OggVorbisEncoder.VorbisInfo info = OggVorbisEncoder.VorbisInfo.InitVariableBitRate(channels, sampleRate, 0.5f);

            // A fixed serial is fine since each fixture file gets its own independent stream, and keeps generation deterministic.
            OggVorbisEncoder.OggStream oggStream = new(serialNumber: 1);

            OggVorbisEncoder.Comments comments = new();
            comments.AddTag("ARTIST", "SoftwareSoundTests");

            oggStream.PacketIn(OggVorbisEncoder.HeaderPacketBuilder.BuildInfoPacket(info));
            oggStream.PacketIn(OggVorbisEncoder.HeaderPacketBuilder.BuildCommentsPacket(comments));
            oggStream.PacketIn(OggVorbisEncoder.HeaderPacketBuilder.BuildBooksPacket(info));
            FlushPages(oggStream, outputData, force: true);

            OggVorbisEncoder.ProcessingState processingState = OggVorbisEncoder.ProcessingState.Create(info);
            const int WriteBufferSize = 512;
            for (int readIndex = 0; readIndex < sampleCount; readIndex += WriteBufferSize)
            {
                int length = Math.Min(WriteBufferSize, sampleCount - readIndex);
                processingState.WriteData(samples, length, readIndex);

                while (!oggStream.Finished && processingState.PacketOut(out OggVorbisEncoder.OggPacket packet))
                {
                    oggStream.PacketIn(packet);
                    FlushPages(oggStream, outputData, force: false);
                }
            }

            processingState.WriteEndOfStream();
            while (!oggStream.Finished && processingState.PacketOut(out OggVorbisEncoder.OggPacket packet))
            {
                oggStream.PacketIn(packet);
                FlushPages(oggStream, outputData, force: false);
            }

            FlushPages(oggStream, outputData, force: true);

            return outputData.ToArray();
        }

        private static void FlushPages(OggVorbisEncoder.OggStream oggStream, Stream output, bool force)
        {
            while (oggStream.PageOut(out OggVorbisEncoder.OggPage page, force))
            {
                output.Write(page.Header, 0, page.Header.Length);
                output.Write(page.Body, 0, page.Body.Length);
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

// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Runtime.InteropServices;
using System.Text;

namespace SharpKind.Audio.Tests;

// Small, self-authored audio files for the tests: a minimal MIDI file, SoundFont2 and WAV.
internal static class AudioFixtures
{
    // SoundFont generator numbers the fixtures use.
    internal const ushort GenInstrument = 41;
    internal const ushort GenKeyRange = 43;
    internal const ushort GenCoarseTune = 51;
    internal const ushort GenSampleId = 53;
    internal const ushort GenSampleModes = 54;
    internal const ushort GenExclusiveClass = 57;

    // A single note-on/note-off SMF (format 0, one track): key 60 vel 100, off after 240 ticks (0.25s at default 120bpm/480 ticks-per-quarter).
    internal static byte[] BuildMinimalMidiFile()
        => BuildMidiFile(
            format: 0,
            division: 480,
            [
                0x00, 0x90, 0x3C, 0x64, // delta 0, note on ch0 key60 vel100
                0x81, 0x70, 0x80, 0x3C, 0x40, // delta 240 (varlen), note off key60 vel64
                0x00, 0xFF, 0x2F, 0x00, // delta 0, end of track
            ]);

    // An SMF of the given tracks, each the bytes of its events.
    internal static byte[] BuildMidiFile(int format, int division, params byte[][] tracks)
    {
        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("MThd"));
            WriteBigEndianInt32(writer, 6);
            WriteBigEndianInt16(writer, (short)format);
            WriteBigEndianInt16(writer, (short)tracks.Length);
            WriteBigEndianInt16(writer, (short)division);

            foreach (byte[] track in tracks)
            {
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                WriteBigEndianInt32(writer, track.Length);
                writer.Write(track);
            }
        }

        return stream.ToArray();
    }

    // One sample (a second of a 440 Hz sine, root key 60, looped whole), one instrument and one preset (bank 0, patch 0).
    internal static byte[] BuildMinimalSoundFont()
        => BuildSoundFont([(0, 0, [[(GenInstrument, 0)]])], [[(GenSampleId, 0)]]);

    // A SoundFont with the minimal font's one sample and one instrument, whose zones are given, and the given presets,
    // all of whose zones play that instrument unless a zone names it itself.
    internal static byte[] BuildSoundFont(
        (int Bank, int Patch, (ushort Type, short Value)[][] Zones)[] presets,
        (ushort Type, short Value)[][] instrumentZones)
    {
        short[] pcm = GenerateSinePcm16(SoftwareSound.SampleRate, frequencyHz: 440, amplitude: 0.5);

        byte[] infoList = BuildListChunk("INFO", BuildInfoContent());

        // The specification asks for 46 zero samples after each sample.
        short[] padded = new short[pcm.Length + 46];
        pcm.CopyTo(padded.AsSpan());
        byte[] sdtaList = BuildListChunk("sdta", BuildChunk("smpl", MemoryMarshal.AsBytes(padded).ToArray()));

        (ushort Type, short Value)[][] presetZones = [.. presets.SelectMany(p => p.Zones)];
        int[] presetBagStarts = new int[presets.Length + 1];
        for (int i = 0; i < presets.Length; i++)
        {
            presetBagStarts[i + 1] = presetBagStarts[i] + presets[i].Zones.Length;
        }

        byte[] pdtaContent =
        [
            .. BuildChunk("phdr", BuildPresetHeaders(presets, presetBagStarts)),
            .. BuildChunk("pbag", BuildZoneBags(presetZones)),
            .. BuildChunk("pgen", BuildGenerators(presetZones)),
            .. BuildChunk("inst", BuildInstrumentHeaders(instrumentZones.Length)),
            .. BuildChunk("ibag", BuildZoneBags(instrumentZones)),
            .. BuildChunk("igen", BuildGenerators(instrumentZones)),
            .. BuildChunk("shdr", BuildSampleHeaders(pcm.Length)),
        ];
        byte[] pdtaList = BuildListChunk("pdta", pdtaContent);

        byte[] body = [.. Encoding.ASCII.GetBytes("sfbk"), .. infoList, .. sdtaList, .. pdtaList];
        return BuildChunk("RIFF", body);
    }

    // Reads a SoundFont built in memory, through a temporary file.
    internal static SoundFonts.SoundFont ReadSoundFont(byte[] file)
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, file);
            return SoundFonts.SoundFont.Read(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // A key or velocity range generator's value.
    internal static short Range(int low, int high) => (short)(low | (high << 8));

    internal static byte[] BuildChunk(string chunkId, byte[] content)
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

    internal static byte[] BuildListChunk(string listType, byte[] content)
    {
        byte[] combined = [.. Encoding.ASCII.GetBytes(listType), .. content];
        return BuildChunk("LIST", combined);
    }

    // Matches the format real .wav assets are authored in: format tag 3 (IEEE float), stereo, 32-bit, at the mixer's own sample rate.
    internal static byte[] BuildTestWavFloat32(double frequencyHz, double durationSeconds, float amplitude)
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

    // Matches the format Elite's .wav effects are authored in: format tag 1 (PCM), mono, 16-bit.
    internal static byte[] BuildTestWavPcm16Mono(double frequencyHz, double durationSeconds, float amplitude, int sampleRate = 44100)
    {
        int frameCount = (int)(durationSeconds * sampleRate);
        int dataLength = frameCount * sizeof(short);

        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataLength);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // format tag: PCM
            writer.Write((short)1); // mono
            writer.Write(sampleRate);
            writer.Write(sampleRate * sizeof(short)); // byte rate
            writer.Write((short)sizeof(short)); // block align
            writer.Write((short)16); // bits per sample

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);

            for (int i = 0; i < frameCount; i++)
            {
                writer.Write((short)(amplitude * short.MaxValue * MathF.Sin(2f * MathF.PI * (float)frequencyHz * i / sampleRate)));
            }
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

    private static byte[] BuildPresetHeaders((int Bank, int Patch, (ushort Type, short Value)[][] Zones)[] presets, int[] bagStarts)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        for (int i = 0; i <= presets.Length; i++)
        {
            bool terminator = i == presets.Length;
            writer.Write(Ascii20(terminator ? "EOP" : "Test" + i));
            writer.Write((ushort)(terminator ? 0 : presets[i].Patch));
            writer.Write((ushort)(terminator ? 0 : presets[i].Bank));
            writer.Write((ushort)bagStarts[i]);
            writer.Write(0); // library
            writer.Write(0); // genre
            writer.Write(0); // morphology
        }

        return stream.ToArray();
    }

    private static byte[] BuildInstrumentHeaders(int zoneCount)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write(Ascii20("Test"));
        writer.Write((ushort)0); // zoneStartIndex

        writer.Write(Ascii20("EOI"));
        writer.Write((ushort)zoneCount); // closes instrument 0's zones

        return stream.ToArray();
    }

    // Shared by pbag and ibag: each zone's first generator, then the terminator recording the total generator count.
    private static byte[] BuildZoneBags((ushort Type, short Value)[][] zones)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        int generatorIndex = 0;
        foreach ((ushort Type, short Value)[] zone in zones)
        {
            writer.Write((ushort)generatorIndex);
            writer.Write((ushort)0); // modulatorIndex (unused)
            generatorIndex += zone.Length;
        }

        writer.Write((ushort)generatorIndex);
        writer.Write((ushort)0);

        return stream.ToArray();
    }

    // Shared by pgen and igen: every zone's generators in order, then the required (discarded) terminator record.
    private static byte[] BuildGenerators((ushort Type, short Value)[][] zones)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        foreach ((ushort type, short value) in zones.SelectMany(z => z))
        {
            writer.Write(type);
            writer.Write(value);
        }

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
}

// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace SharpKind.Audio.Wav;

internal static class WavDecoder
{
    // Manual RIFF/WAVE chunk walk (not fixed 44-byte header) since some files carry extra chunks (e.g. LIST/INFO) before 'data'. Only PCM16 (tag 1) and float32 (tag 3) are supported.
    internal static float[] Decode(string path)
    {
        ReadOnlySpan<byte> span = File.ReadAllBytes(path);

        if (span.Length < 12 || !span[..4].SequenceEqual("RIFF"u8) || !span.Slice(8, 4).SequenceEqual("WAVE"u8))
        {
            throw new SharpKindException($"'{path}' is not a valid WAV file.");
        }

        ushort formatTag = 0;
        ushort channels = 0;
        uint sampleRate = 0;
        ushort bitsPerSample = 0;
        ReadOnlySpan<byte> data = default;

        int offset = 12;
        while (offset + 8 <= span.Length)
        {
            ReadOnlySpan<byte> chunkId = span.Slice(offset, 4);
            uint chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 4, 4));
            int chunkStart = offset + 8;
            int chunkLength = (int)Math.Min(chunkSize, (uint)Math.Max(span.Length - chunkStart, 0));

            if (chunkId.SequenceEqual("fmt "u8))
            {
                ReadOnlySpan<byte> fmt = span.Slice(chunkStart, chunkLength);
                if (fmt.Length < 16)
                {
                    throw new SharpKindException($"'{path}' has a truncated 'fmt ' chunk.");
                }

                formatTag = BinaryPrimitives.ReadUInt16LittleEndian(fmt);
                channels = BinaryPrimitives.ReadUInt16LittleEndian(fmt[2..]);
                sampleRate = BinaryPrimitives.ReadUInt32LittleEndian(fmt[4..]);
                bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(fmt[14..]);
            }
            else if (chunkId.SequenceEqual("data"u8))
            {
                data = span.Slice(chunkStart, chunkLength);
            }

            // Chunks are word-aligned: an odd-sized chunk is followed by a single pad byte.
            offset = chunkStart + chunkLength + ((chunkSize % 2 == 1) ? 1 : 0);
        }

        return data.IsEmpty
            ? throw new SharpKindException($"'{path}' has no 'data' chunk.")
            : ToStereoFloat(path, formatTag, channels, sampleRate, bitsPerSample, data);
    }

    private static float[] ToStereoFloat(
        string path,
        ushort formatTag,
        ushort channels,
        uint sampleRate,
        ushort bitsPerSample,
        in ReadOnlySpan<byte> data)
    {
        // No sample-rate converter: assets must already be at the mixer's rate.
        if (sampleRate != SoftwareSound.SampleRate)
        {
            throw new SharpKindException($"'{path}' is at {sampleRate} Hz; only {SoftwareSound.SampleRate} Hz is supported.");
        }

        if (channels is not (1 or 2))
        {
            throw new SharpKindException($"'{path}' is {channels}-channel audio; only mono and stereo are supported.");
        }

        float[] samples = (formatTag, bitsPerSample) switch
        {
            (3, 32) => MemoryMarshal.Cast<byte, float>(data).ToArray(),
            (1, 16) => DecodePcm16(data),
            _ => throw new SharpKindException($"'{path}' uses an unsupported WAV format (tag {formatTag}, {bitsPerSample}-bit)."),
        };

        return channels == 1 ? MonoToStereo(samples) : samples;
    }

    private static float[] DecodePcm16(in ReadOnlySpan<byte> data)
    {
        ReadOnlySpan<short> samples = MemoryMarshal.Cast<byte, short>(data);
        float[] result = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            result[i] = samples[i] / 32768f;
        }

        return result;
    }

    private static float[] MonoToStereo(float[] mono)
    {
        float[] stereo = new float[mono.Length * 2];
        for (int i = 0; i < mono.Length; i++)
        {
            stereo[i * 2] = mono[i];
            stereo[(i * 2) + 1] = mono[i];
        }

        return stereo;
    }
}

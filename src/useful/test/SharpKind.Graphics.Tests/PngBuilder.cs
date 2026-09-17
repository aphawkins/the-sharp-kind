// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Buffers.Binary;
using System.IO.Compression;

namespace SharpKind.Graphics.Tests;

// Chunk CRCs are written as zero: PngReader skips them, and computing real ones would just test the test's own CRC implementation.
internal static class PngBuilder
{
    public static byte[] Build(
        int width,
        int height,
        byte bitDepth,
        byte colourType,
        byte[] scanlines,
        byte[]? palette = null,
        byte[]? transparency = null,
        byte interlace = 0)
    {
        using MemoryStream file = new();
        file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = bitDepth;
        header[9] = colourType;
        header[12] = interlace;
        WriteChunk(file, "IHDR"u8, header);

        if (palette is not null)
        {
            WriteChunk(file, "PLTE"u8, palette);
        }

        if (transparency is not null)
        {
            WriteChunk(file, "tRNS"u8, transparency);
        }

        WriteChunk(file, "IDAT"u8, Deflate(scanlines));
        WriteChunk(file, "IEND"u8, []);

        return file.ToArray();
    }

    private static void WriteChunk(Stream file, in ReadOnlySpan<byte> type, byte[] data)
    {
        byte[] length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        file.Write(length);
        file.Write(type);
        file.Write(data);
        file.Write(new byte[4]);
    }

    private static byte[] Deflate(byte[] scanlines)
    {
        using MemoryStream compressed = new();

        using (ZLibStream zlib = new(compressed, CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(scanlines);
        }

        return compressed.ToArray();
    }
}

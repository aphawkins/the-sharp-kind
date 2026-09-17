// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Buffers.Binary;

namespace SharpKind.Graphics.Tests;

// Deliberately hand-rolled rather than routed through BitmapWriter, which only emits 32bpp.
internal static class BmpBuilder
{
    private const int FileHeaderSize = 14;
    private const int InfoHeaderSize = 40;

    public static byte[] Build(
        int width,
        int signedHeight,
        short bitsPerPixel,
        byte[] pixelData,
        uint[]? palette = null,
        int compression = 0)
    {
        int paletteLength = palette?.Length ?? 0;
        int dataOffset = FileHeaderSize + InfoHeaderSize + (paletteLength * 4);
        byte[] file = new byte[dataOffset + pixelData.Length];

        file[0] = (byte)'B';
        file[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(2), file.Length);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(10), dataOffset);

        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(14), InfoHeaderSize);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(18), width);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(22), signedHeight);
        BinaryPrimitives.WriteInt16LittleEndian(file.AsSpan(26), 1);
        BinaryPrimitives.WriteInt16LittleEndian(file.AsSpan(28), bitsPerPixel);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(30), compression);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(46), paletteLength);

        for (int i = 0; i < paletteLength; i++)
        {
            int entry = FileHeaderSize + InfoHeaderSize + (i * 4);
            file[entry] = (byte)(palette![i] & 0xFF);
            file[entry + 1] = (byte)((palette[i] >> 8) & 0xFF);
            file[entry + 2] = (byte)((palette[i] >> 16) & 0xFF);
        }

        pixelData.CopyTo(file, dataOffset);
        return file;
    }
}

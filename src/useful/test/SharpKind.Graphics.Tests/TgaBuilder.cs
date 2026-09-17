// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Buffers.Binary;

namespace SharpKind.Graphics.Tests;

// Builds minimal TGAs so the decoder tests can cover the image types, depths
// and row orders no committed asset uses. Version 1 files, with no footer -
// which is the shape the format is usually met in, and the one IsTga has to
// recognise from the header alone.
internal static class TgaBuilder
{
    private const int HeaderSize = 18;

    public static byte[] Build(
        int width,
        int height,
        byte imageType,
        byte pixelDepth,
        byte[] pixelData,
        byte[]? colourMap = null,
        byte colourMapDepth = 24,
        byte descriptor = 0)
    {
        int colourMapEntries = colourMap is null ? 0 : colourMap.Length / (colourMapDepth / 8);
        int colourMapBytes = colourMap?.Length ?? 0;
        byte[] file = new byte[HeaderSize + colourMapBytes + pixelData.Length];

        file[1] = (byte)(colourMap is null ? 0 : 1);
        file[2] = imageType;
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(5), (ushort)colourMapEntries);
        file[7] = colourMap is null ? (byte)0 : colourMapDepth;
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(12), (ushort)width);
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(14), (ushort)height);
        file[16] = pixelDepth;
        file[17] = descriptor;

        colourMap?.CopyTo(file, HeaderSize);
        pixelData.CopyTo(file, HeaderSize + colourMapBytes);

        return file;
    }
}

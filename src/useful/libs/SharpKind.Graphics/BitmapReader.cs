// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Buffers.Binary;

namespace SharpKind.Graphics;

// Decodes uncompressed Windows BMPs: 1/4/8bpp palettised, 24bpp BGR, 32bpp BGRA, bottom-up or top-down. 32bpp is always read as BGRA (ignoring BI_BITFIELDS masks), matching what BitmapWriter emits.
public static class BitmapReader
{
    private const int FileHeaderSize = 14;
    private const int DataOffsetOffset = 10;
    private const int DibHeaderSizeOffset = 14;
    private const int WidthOffset = 18;
    private const int HeightOffset = 22;
    private const int BitsPerPixelOffset = 28;
    private const int CompressionOffset = 30;
    private const int PaletteCountOffset = 46;
    private const int InfoHeaderSize = 40;
    private const int CompressionRgb = 0;
    private const int CompressionBitfields = 3;

    public static FastBitmap Read(string path) => Decode(File.ReadAllBytes(path));

    internal static bool IsBmp(byte[] bytes) => bytes.Length >= 2 && bytes[0] == 'B' && bytes[1] == 'M';

    internal static FastBitmap Decode(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return new(0, 0);
        }

        if (!IsBmp(bytes))
        {
            throw new SharpKindException("Identifier is incorrect: not a BMP file.");
        }

        int dibHeaderSize = ReadInt32(bytes, DibHeaderSizeOffset);
        int width = ReadInt32(bytes, WidthOffset);
        int signedHeight = ReadInt32(bytes, HeightOffset);
        short bitsPerPixel = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(BitsPerPixelOffset));
        ValidateHeader(dibHeaderSize, ReadInt32(bytes, CompressionOffset), width, signedHeight, bitsPerPixel);

        int height = Math.Abs(signedHeight);
        uint[] palette = bitsPerPixel <= 8
            ? ReadPalette(bytes, FileHeaderSize + dibHeaderSize, bitsPerPixel)
            : [];

        // Rows are padded out to a 4-byte boundary.
        int stride = ((width * bitsPerPixel) + 31) / 32 * 4;
        int dataOffset = ReadInt32(bytes, DataOffsetOffset);
        if (dataOffset < 0 || dataOffset + ((long)stride * height) > bytes.Length)
        {
            throw new SharpKindException("BMP pixel data extends past the end of the file.");
        }

        // Negative height means rows are stored top-down; positive (the usual) means bottom-up, so rows need flipping.
        bool topDown = signedHeight < 0;
        uint[] pixels = new uint[width * height];

        for (int y = 0; y < height; y++)
        {
            int sourceRow = topDown ? y : height - y - 1;
            int rowOffset = dataOffset + (sourceRow * stride);
            int destinationRow = y * width;

            for (int x = 0; x < width; x++)
            {
                pixels[destinationRow + x] = bitsPerPixel switch
                {
                    32 => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(rowOffset + (x * 4))),
                    24 => Opaque(bytes[rowOffset + (x * 3) + 2], bytes[rowOffset + (x * 3) + 1], bytes[rowOffset + (x * 3)]),
                    _ => palette[PaletteIndex(bytes, rowOffset, x, bitsPerPixel)],
                };
            }
        }

        return new(width, height, pixels);
    }

    private static void ValidateHeader(int dibHeaderSize, int compression, int width, int signedHeight, short bitsPerPixel)
    {
        if (dibHeaderSize < InfoHeaderSize)
        {
            throw new SharpKindException($"Unsupported BMP header size: {dibHeaderSize}. BITMAPCOREHEADER is not supported.");
        }

        if (compression is not (CompressionRgb or CompressionBitfields))
        {
            throw new SharpKindException($"Unsupported BMP compression: {compression}. Only uncompressed BMPs are supported.");
        }

        if (width <= 0 || signedHeight == 0)
        {
            throw new SharpKindException($"Invalid BMP dimensions: {width}x{signedHeight}.");
        }

        if (bitsPerPixel is not (1 or 4 or 8 or 24 or 32))
        {
            throw new SharpKindException($"Unsupported BMP bit depth: {bitsPerPixel}.");
        }
    }

    private static int ReadInt32(byte[] bytes, int offset) => BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset));

    private static uint Opaque(byte red, byte green, byte blue)
        => 0xFF00_0000u | ((uint)red << 16) | ((uint)green << 8) | blue;

    private static uint[] ReadPalette(byte[] bytes, int offset, short bitsPerPixel)
    {
        int declared = ReadInt32(bytes, PaletteCountOffset);
        int count = declared > 0 ? declared : 1 << bitsPerPixel;

        if (offset + (count * 4) > bytes.Length)
        {
            throw new SharpKindException("BMP palette extends past the end of the file.");
        }

        uint[] palette = new uint[1 << bitsPerPixel];
        for (int i = 0; i < count; i++)
        {
            // Palette entries are BGR plus a reserved byte, so they are
            // always opaque - BMP carries no palette alpha.
            palette[i] = Opaque(bytes[offset + (i * 4) + 2], bytes[offset + (i * 4) + 1], bytes[offset + (i * 4)]);
        }

        return palette;
    }

    private static int PaletteIndex(byte[] bytes, int rowOffset, int x, short bitsPerPixel)
    {
        int perByte = 8 / bitsPerPixel;
        byte packed = bytes[rowOffset + (x / perByte)];
        int pixelInByte = x % perByte;
        int shift = 8 - bitsPerPixel - (pixelInByte * bitsPerPixel);
        return (packed >> shift) & ((1 << bitsPerPixel) - 1);
    }
}

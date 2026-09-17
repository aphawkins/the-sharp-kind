// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Buffers.Binary;

namespace SharpKind.Graphics;

// Decodes Truevision TGA: colour-mapped, true-colour and greyscale, each
// either uncompressed or run-length encoded, at 8, 16, 24 or 32 bits per
// pixel, in either row order.
//
// TGA has no signature at the start of the file - the format predates the
// habit - so IsTga reads the 18-byte header and asks whether it could be one.
// Version 2 files end with a footer that does identify them, and that is
// checked first, but the files this was added for are version 1 and have
// none. ImageReader therefore tries PNG and BMP before it tries this.
public static class TgaReader
{
    private const int HeaderSize = 18;
    private const int FooterSize = 26;

    private const int IdLengthOffset = 0;
    private const int ColourMapTypeOffset = 1;
    private const int ImageTypeOffset = 2;
    private const int ColourMapFirstOffset = 3;
    private const int ColourMapLengthOffset = 5;
    private const int ColourMapDepthOffset = 7;
    private const int WidthOffset = 12;
    private const int HeightOffset = 14;
    private const int PixelDepthOffset = 16;
    private const int DescriptorOffset = 17;

    private const int ColourMapped = 1;
    private const int TrueColour = 2;
    private const int Greyscale = 3;
    private const int RunLengthColourMapped = 9;
    private const int RunLengthTrueColour = 10;
    private const int RunLengthGreyscale = 11;

    // Bit 5 of the descriptor: set means the rows are stored top to bottom.
    private const byte TopToBottom = 0x20;

    // Bit 4: set means each row is stored right to left.
    private const byte RightToLeft = 0x10;

    // The high bit of a packet's header byte marks a run rather than a
    // literal, and the low seven are the count less one.
    private const byte RunPacket = 0x80;
    private const byte PacketCountMask = 0x7F;

    private static ReadOnlySpan<byte> FooterSignature => "TRUEVISION-XFILE"u8;

    public static FastBitmap Read(string path) => Decode(File.ReadAllBytes(path));

    internal static bool IsTga(byte[] bytes)
    {
        if (bytes.Length < HeaderSize)
        {
            return false;
        }

        // A version 2 file says so outright, 18 bytes from the end.
        if (bytes.Length >= FooterSize
            && bytes.AsSpan(bytes.Length - 18, FooterSignature.Length).SequenceEqual(FooterSignature))
        {
            return true;
        }

        // Otherwise the header has to be one a TGA could have had. Every
        // field below is a small closed set, so a file of some other format
        // reaching this point is very unlikely to satisfy all of them.
        byte colourMapType = bytes[ColourMapTypeOffset];
        byte imageType = bytes[ImageTypeOffset];
        byte pixelDepth = bytes[PixelDepthOffset];
        int colourMapLength = ReadUInt16(bytes, ColourMapLengthOffset);

        if (colourMapType > 1 || !IsSupportedType(imageType) || !IsSupportedDepth(pixelDepth))
        {
            return false;
        }

        // A file that says it has no colour map may not then describe one.
        return (colourMapType != 0 || colourMapLength == 0)
            && ReadUInt16(bytes, WidthOffset) > 0
            && ReadUInt16(bytes, HeightOffset) > 0
            && PixelDataOffset(bytes) <= bytes.Length;
    }

    internal static FastBitmap Decode(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return new(0, 0);
        }

        if (!IsTga(bytes))
        {
            throw new SharpKindException("Identifier is incorrect: not a TGA file.");
        }

        byte imageType = bytes[ImageTypeOffset];
        byte pixelDepth = bytes[PixelDepthOffset];
        int width = ReadUInt16(bytes, WidthOffset);
        int height = ReadUInt16(bytes, HeightOffset);
        ValidateHeader(imageType, pixelDepth, width, height);

        uint[] colourMap = bytes[ColourMapTypeOffset] == 1 ? ReadColourMap(bytes) : [];
        int bytesPerPixel = pixelDepth / 8;
        byte[] samples = ReadSamples(bytes, width * height * bytesPerPixel);

        byte descriptor = bytes[DescriptorOffset];
        bool topToBottom = (descriptor & TopToBottom) != 0;
        bool rightToLeft = (descriptor & RightToLeft) != 0;
        uint[] pixels = new uint[width * height];

        for (int y = 0; y < height; y++)
        {
            // TGA's own origin is the bottom-left unless the descriptor says
            // otherwise, and FastBitmap's is the top-left, so the usual file
            // has its rows read back to front.
            int sourceRow = topToBottom ? y : height - y - 1;
            int rowOffset = sourceRow * width * bytesPerPixel;
            int destinationRow = y * width;

            for (int x = 0; x < width; x++)
            {
                int sourceColumn = rightToLeft ? width - x - 1 : x;
                pixels[destinationRow + x] = ToArgb(
                    samples,
                    rowOffset + (sourceColumn * bytesPerPixel),
                    pixelDepth,
                    imageType,
                    colourMap);
            }
        }

        return new(width, height, pixels);
    }

    private static bool IsSupportedType(byte imageType)
        => imageType is ColourMapped or TrueColour or Greyscale
            or RunLengthColourMapped or RunLengthTrueColour or RunLengthGreyscale;

    private static bool IsSupportedDepth(byte pixelDepth) => pixelDepth is 8 or 16 or 24 or 32;

    private static bool IsRunLengthEncoded(byte imageType)
        => imageType is RunLengthColourMapped or RunLengthTrueColour or RunLengthGreyscale;

    private static bool IsColourMapped(byte imageType)
        => imageType is ColourMapped or RunLengthColourMapped;

    private static void ValidateHeader(byte imageType, byte pixelDepth, int width, int height)
    {
        if (!IsSupportedType(imageType))
        {
            throw new SharpKindException($"Unsupported TGA image type: {imageType}.");
        }

        if (!IsSupportedDepth(pixelDepth))
        {
            throw new SharpKindException($"Unsupported TGA pixel depth: {pixelDepth}.");
        }

        if (width <= 0 || height <= 0)
        {
            throw new SharpKindException($"Invalid TGA dimensions: {width}x{height}.");
        }
    }

    private static int ReadUInt16(byte[] bytes, int offset)
        => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset));

    // The header, the optional image ID, then the colour map if there is one.
    private static int PixelDataOffset(byte[] bytes)
        => HeaderSize
            + bytes[IdLengthOffset]
            + (ReadUInt16(bytes, ColourMapLengthOffset) * (bytes[ColourMapDepthOffset] / 8));

    private static uint[] ReadColourMap(byte[] bytes)
    {
        int first = ReadUInt16(bytes, ColourMapFirstOffset);
        int length = ReadUInt16(bytes, ColourMapLengthOffset);
        int entryBytes = bytes[ColourMapDepthOffset] / 8;
        int offset = HeaderSize + bytes[IdLengthOffset];

        if (entryBytes is < 2 or > 4)
        {
            throw new SharpKindException($"Unsupported TGA colour map depth: {bytes[ColourMapDepthOffset]}.");
        }

        if (offset + (length * entryBytes) > bytes.Length)
        {
            throw new SharpKindException("TGA colour map extends past the end of the file.");
        }

        // Sized to hold the highest index the image can name, so a map that
        // starts part-way up - which the first-entry field allows - is still
        // indexed by the value in the pixel rather than by an offset from it.
        uint[] colourMap = new uint[first + length];
        for (int i = 0; i < length; i++)
        {
            colourMap[first + i] = ToArgb(bytes, offset + (i * entryBytes), (byte)(entryBytes * 8));
        }

        return colourMap;
    }

    // The pixel data, unpacked if it is run-length encoded. An encoded file is
    // expanded here rather than during the row walk, so the walk indexes both
    // kinds the same way.
    private static byte[] ReadSamples(byte[] bytes, int expectedLength)
    {
        int offset = PixelDataOffset(bytes);

        return IsRunLengthEncoded(bytes[ImageTypeOffset])
            ? ExpandRunLength(bytes, offset, expectedLength, bytes[PixelDepthOffset] / 8)
            : offset + expectedLength > bytes.Length
                ? throw new SharpKindException("TGA pixel data extends past the end of the file.")
                : bytes.AsSpan(offset, expectedLength).ToArray();
    }

    private static byte[] ExpandRunLength(byte[] bytes, int offset, int expectedLength, int bytesPerPixel)
    {
        byte[] samples = new byte[expectedLength];
        int written = 0;

        while (written < expectedLength)
        {
            if (offset >= bytes.Length)
            {
                throw new SharpKindException("TGA pixel data ends part-way through a packet.");
            }

            byte packet = bytes[offset++];
            int count = (packet & PacketCountMask) + 1;
            int wanted = count * bytesPerPixel;

            if (written + wanted > expectedLength)
            {
                throw new SharpKindException("TGA run-length data describes more pixels than the image has.");
            }

            offset += (packet & RunPacket) != 0
                ? CopyRun(bytes, offset, samples, written, count, bytesPerPixel)
                : CopyLiteral(bytes, offset, samples, written, wanted);

            written += wanted;
        }

        return samples;
    }

    // One pixel, repeated. Returns how much of the file it consumed.
    private static int CopyRun(byte[] bytes, int offset, byte[] samples, int written, int count, int bytesPerPixel)
    {
        if (offset + bytesPerPixel > bytes.Length)
        {
            throw new SharpKindException("TGA pixel data ends part-way through a run.");
        }

        for (int i = 0; i < count; i++)
        {
            bytes.AsSpan(offset, bytesPerPixel).CopyTo(samples.AsSpan(written + (i * bytesPerPixel)));
        }

        return bytesPerPixel;
    }

    // The pixels one after another. Returns how much of the file it consumed.
    private static int CopyLiteral(byte[] bytes, int offset, byte[] samples, int written, int wanted)
    {
        if (offset + wanted > bytes.Length)
        {
            throw new SharpKindException("TGA pixel data ends part-way through a literal run.");
        }

        bytes.AsSpan(offset, wanted).CopyTo(samples.AsSpan(written));

        return wanted;
    }

    private static uint ToArgb(byte[] samples, int offset, byte pixelDepth, byte imageType, uint[] colourMap)
    {
        if (IsColourMapped(imageType))
        {
            int index = pixelDepth == 8 ? samples[offset] : ReadUInt16(samples, offset);
            return (uint)index < (uint)colourMap.Length ? colourMap[index] : 0;
        }

        // Greyscale carries one sample that is all three channels.
        if (pixelDepth == 8)
        {
            byte grey = samples[offset];
            return Opaque(grey, grey, grey);
        }

        return ToArgb(samples, offset, pixelDepth);
    }

    // One true-colour sample, in the depth given. Shared with the colour map,
    // whose entries carry the same three layouts.
    private static uint ToArgb(byte[] samples, int offset, byte depth)
    {
        switch (depth)
        {
            case 32:
                return BinaryPrimitives.ReadUInt32LittleEndian(samples.AsSpan(offset));

            case 24:
                return Opaque(samples[offset + 2], samples[offset + 1], samples[offset]);

            default:
                // 15 and 16 bit are the same five bits per channel; the spare
                // top bit is an attribute bit that files disagree about, so it
                // is ignored and the pixel is opaque.
                int packed = ReadUInt16(samples, offset);
                byte red = Expand5((packed >> 10) & 0x1F);
                byte green = Expand5((packed >> 5) & 0x1F);
                byte blue = Expand5(packed & 0x1F);
                return Opaque(red, green, blue);
        }
    }

    // Five bits to eight, so full scale stays full scale rather than landing
    // on 248.
    private static byte Expand5(int value) => (byte)((value << 3) | (value >> 2));

    private static uint Opaque(byte red, byte green, byte blue)
        => 0xFF00_0000u | ((uint)red << 16) | ((uint)green << 8) | blue;
}

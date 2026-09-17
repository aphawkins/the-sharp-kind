// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Buffers.Binary;
using System.IO.Compression;

namespace SharpKind.Graphics;

// Decodes non-interlaced PNGs on the framework's own ZLibStream, not a third-party library.
// Interlaced (Adam7) files are rejected outright; chunk CRCs are skipped since a corrupt asset surfaces as a decode failure anyway.
public static class PngReader
{
    private const int SignatureLength = 8;
    private const int ChunkHeaderLength = 8;
    private const int ChunkCrcLength = 4;
    private const int HeaderChunkLength = 13;

    private const byte Greyscale = 0;
    private const byte Truecolour = 2;
    private const byte Indexed = 3;
    private const byte GreyscaleAlpha = 4;
    private const byte TruecolourAlpha = 6;

    private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static FastBitmap Read(string path) => Decode(File.ReadAllBytes(path));

    internal static bool IsPng(byte[] bytes)
        => bytes.Length >= SignatureLength && bytes.AsSpan(0, SignatureLength).SequenceEqual(Signature);

    internal static FastBitmap Decode(byte[] bytes)
    {
        if (!IsPng(bytes))
        {
            throw new SharpKindException("Identifier is incorrect: not a PNG file.");
        }

        int width = 0;
        int height = 0;
        byte bitDepth = 0;
        byte colourType = 0;
        uint[] palette = [];
        byte[] paletteAlpha = [];
        int[] colourKey = [];
        using MemoryStream compressed = new();

        int position = SignatureLength;
        while (position + ChunkHeaderLength <= bytes.Length)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(position));
            ReadOnlySpan<byte> chunkType = bytes.AsSpan(position + 4, 4);
            int data = position + ChunkHeaderLength;

            if (length < 0 || data + length + ChunkCrcLength > bytes.Length)
            {
                throw new SharpKindException("PNG chunk extends past the end of the file.");
            }

            if (chunkType.SequenceEqual("IHDR"u8))
            {
                (width, height, bitDepth, colourType) = ReadHeader(bytes, data, length);
            }
            else if (chunkType.SequenceEqual("PLTE"u8))
            {
                palette = ReadPalette(bytes, data, length);
            }
            else if (chunkType.SequenceEqual("tRNS"u8))
            {
                (paletteAlpha, colourKey) = ReadTransparency(bytes, data, length, colourType);
            }
            else if (chunkType.SequenceEqual("IDAT"u8))
            {
                compressed.Write(bytes, data, length);
            }
            else if (chunkType.SequenceEqual("IEND"u8))
            {
                break;
            }

            position = data + length + ChunkCrcLength;
        }

        if (width == 0)
        {
            throw new SharpKindException("PNG is missing its IHDR chunk.");
        }

        int channels = ChannelCount(colourType);
        ValidateBitDepth(colourType, bitDepth);

        if (colourType == Indexed && palette.Length == 0)
        {
            throw new SharpKindException("Indexed PNG is missing its PLTE chunk.");
        }

        int bitsPerPixel = channels * bitDepth;
        int stride = ((width * bitsPerPixel) + 7) / 8;
        byte[] raw = Inflate(compressed, height * (stride + 1));
        Unfilter(raw, height, stride, Math.Max(1, bitsPerPixel / 8));

        return new(width, height, ToPixels(raw, width, height, stride, bitDepth, colourType, palette, paletteAlpha, colourKey));
    }

    private static (int Width, int Height, byte BitDepth, byte ColourType) ReadHeader(byte[] bytes, int data, int length)
    {
        if (length < HeaderChunkLength)
        {
            throw new SharpKindException("PNG IHDR chunk is too short.");
        }

        int width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(data));
        int height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(data + 4));
        ValidateHeader(width, height, bytes[data + 10], bytes[data + 11], bytes[data + 12]);

        return (width, height, bytes[data + 8], bytes[data + 9]);
    }

    private static void ValidateHeader(int width, int height, byte compression, byte filter, byte interlace)
    {
        if (width <= 0 || height <= 0)
        {
            throw new SharpKindException($"Invalid PNG dimensions: {width}x{height}.");
        }

        if (compression != 0 || filter != 0)
        {
            throw new SharpKindException($"Unsupported PNG {nameof(compression)} or {nameof(filter)} method.");
        }

        if (interlace != 0)
        {
            throw new SharpKindException("Interlaced PNGs are not supported.");
        }
    }

    private static uint[] ReadPalette(byte[] bytes, int data, int length)
    {
        uint[] palette = new uint[length / 3];
        for (int i = 0; i < palette.Length; i++)
        {
            palette[i] = 0xFF00_0000u
                | ((uint)bytes[data + (i * 3)] << 16)
                | ((uint)bytes[data + (i * 3) + 1] << 8)
                | bytes[data + (i * 3) + 2];
        }

        return palette;
    }

    private static (byte[] PaletteAlpha, int[] ColourKey) ReadTransparency(byte[] bytes, int data, int length, byte colourType)
        => colourType switch
        {
            Indexed => (bytes.AsSpan(data, length).ToArray(), []),
            Greyscale => ([], [BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(data))]),
            Truecolour => (
                [],
                [
                    BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(data)),
                    BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(data + 2)),
                    BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(data + 4)),
                ]),

            // The alpha channel already carries transparency for types 4 and 6.
            _ => ([], []),
        };

    private static int ChannelCount(byte colourType) => colourType switch
    {
        Greyscale or Indexed => 1,
        GreyscaleAlpha => 2,
        Truecolour => 3,
        TruecolourAlpha => 4,
        _ => throw new SharpKindException($"Unsupported PNG colour type: {colourType}."),
    };

    private static void ValidateBitDepth(byte colourType, byte bitDepth)
    {
        bool valid = colourType switch
        {
            Greyscale => bitDepth is 1 or 2 or 4 or 8 or 16,
            Indexed => bitDepth is 1 or 2 or 4 or 8,
            _ => bitDepth is 8 or 16,
        };

        if (!valid)
        {
            throw new SharpKindException($"Unsupported PNG bit depth {bitDepth} for colour type {colourType}.");
        }
    }

    private static byte[] Inflate(MemoryStream compressed, int expectedLength)
    {
        if (compressed.Length == 0)
        {
            throw new SharpKindException("PNG is missing its IDAT chunks.");
        }

        compressed.Position = 0;
        byte[] raw = new byte[expectedLength];

        try
        {
            using ZLibStream zlib = new(compressed, CompressionMode.Decompress, leaveOpen: true);
            zlib.ReadExactly(raw);
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException)
        {
            throw new SharpKindException("Failed to decompress PNG pixel data.", ex);
        }

        return raw;
    }

    // Reversing the filter needs already-reconstructed bytes to the left and above, so this walks forwards, rewriting in place.
    private static void Unfilter(byte[] raw, int height, int stride, int unit)
    {
        for (int y = 0; y < height; y++)
        {
            int row = y * (stride + 1);
            byte filter = raw[row];
            int current = row + 1;
            int previous = current - (stride + 1);

            for (int i = 0; i < stride; i++)
            {
                raw[current + i] = Reconstruct(
                    filter,
                    raw[current + i],
                    i >= unit ? raw[current + i - unit] : 0,
                    y > 0 ? raw[previous + i] : 0,
                    y > 0 && i >= unit ? raw[previous + i - unit] : 0);
            }
        }
    }

    private static byte Reconstruct(byte filter, byte filtered, int left, int above, int aboveLeft) => filter switch
    {
        0 => filtered,
        1 => (byte)(filtered + left),
        2 => (byte)(filtered + above),
        3 => (byte)(filtered + ((left + above) / 2)),
        4 => (byte)(filtered + Paeth(left, above, aboveLeft)),
        _ => throw new SharpKindException($"Unknown PNG filter type: {filter}."),
    };

    private static int Paeth(int left, int above, int aboveLeft)
    {
        int estimate = left + above - aboveLeft;
        int distanceLeft = Math.Abs(estimate - left);
        int distanceAbove = Math.Abs(estimate - above);
        int distanceAboveLeft = Math.Abs(estimate - aboveLeft);

        return distanceLeft <= distanceAbove && distanceLeft <= distanceAboveLeft
            ? left
            : distanceAbove <= distanceAboveLeft ? above : aboveLeft;
    }

    private static uint[] ToPixels(
        byte[] raw,
        int width,
        int height,
        int stride,
        byte bitDepth,
        byte colourType,
        uint[] palette,
        byte[] paletteAlpha,
        int[] colourKey)
    {
        int channels = ChannelCount(colourType);
        uint[] pixels = new uint[width * height];

        for (int y = 0; y < height; y++)
        {
            int row = (y * (stride + 1)) + 1;
            int destinationRow = y * width;

            for (int x = 0; x < width; x++)
            {
                int first = x * channels;

                pixels[destinationRow + x] = colourType switch
                {
                    Greyscale => Grey(Sample(raw, row, first, bitDepth), bitDepth, colourKey),
                    GreyscaleAlpha => Argb(
                        (byte)Sample(raw, row, first + 1, bitDepth),
                        Scale(Sample(raw, row, first, bitDepth), bitDepth)),
                    Truecolour => Colour(raw, row, first, bitDepth, colourKey),
                    TruecolourAlpha => Argb(
                        (byte)Sample(raw, row, first + 3, bitDepth),
                        (byte)Sample(raw, row, first, bitDepth),
                        (byte)Sample(raw, row, first + 1, bitDepth),
                        (byte)Sample(raw, row, first + 2, bitDepth)),
                    _ => Palettised(Sample(raw, row, first, bitDepth), palette, paletteAlpha),
                };
            }
        }

        return pixels;
    }

    // 16-bit samples truncate to their high byte: FastBitmap is 8 bits per channel.
    private static int Sample(byte[] raw, int row, int index, byte bitDepth)
    {
        if (bitDepth == 8)
        {
            return raw[row + index];
        }

        if (bitDepth == 16)
        {
            return raw[row + (index * 2)];
        }

        int perByte = 8 / bitDepth;
        byte packed = raw[row + (index / perByte)];
        int sampleInByte = index % perByte;
        int shift = 8 - bitDepth - (sampleInByte * bitDepth);
        return (packed >> shift) & ((1 << bitDepth) - 1);
    }

    // Spreads a sub-byte sample across the full 0-255 range, so 1-bit white
    // reads as 255 rather than 1.
    private static byte Scale(int sample, byte bitDepth) => bitDepth switch
    {
        8 or 16 => (byte)sample,
        4 => (byte)(sample * 17),
        2 => (byte)(sample * 85),
        _ => (byte)(sample * 255),
    };

    private static uint Grey(int sample, byte bitDepth, int[] colourKey)
    {
        byte level = Scale(sample, bitDepth);
        byte alpha = colourKey.Length == 1 && sample == KeyFor(colourKey[0], bitDepth) ? (byte)0 : (byte)255;
        return Argb(alpha, level, level, level);
    }

    private static uint Colour(byte[] raw, int row, int first, byte bitDepth, int[] colourKey)
    {
        int red = Sample(raw, row, first, bitDepth);
        int green = Sample(raw, row, first + 1, bitDepth);
        int blue = Sample(raw, row, first + 2, bitDepth);

        byte alpha = colourKey.Length == 3
            && red == KeyFor(colourKey[0], bitDepth)
            && green == KeyFor(colourKey[1], bitDepth)
            && blue == KeyFor(colourKey[2], bitDepth)
                ? (byte)0
                : (byte)255;

        return Argb(alpha, (byte)red, (byte)green, (byte)blue);
    }

    // tRNS keys are always stored as 16-bit values; Sample truncates 16-bit
    // samples to their high byte, so the key has to be truncated to match.
    private static int KeyFor(int key, byte bitDepth) => bitDepth == 16 ? key >> 8 : key;

    private static uint Palettised(int index, uint[] palette, byte[] paletteAlpha)
    {
        if (index >= palette.Length)
        {
            throw new SharpKindException($"PNG {nameof(palette)} index {index} is outside its {palette.Length} entries.");
        }

        uint entry = palette[index];

        return index < paletteAlpha.Length
            ? (entry & 0x00FF_FFFFu) | ((uint)paletteAlpha[index] << 24)
            : entry;
    }

    private static uint Argb(byte alpha, byte grey) => Argb(alpha, grey, grey, grey);

    private static uint Argb(byte alpha, byte red, byte green, byte blue)
        => ((uint)alpha << 24) | ((uint)red << 16) | ((uint)green << 8) | blue;
}

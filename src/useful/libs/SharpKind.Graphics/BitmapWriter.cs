// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics;

// Writes the 32bpp BGRA bottom-up BMP format BitmapReader reads back, as a standard BITMAPV5HEADER (masks + "Win " colour space) so it also opens in ordinary image viewers.
public static class BitmapWriter
{
    private const int FileHeaderSize = 14;
    private const int DibHeaderSize = 124;
    private const uint WindowsColorSpace = 0x5773_6E20; // "Win " (LCS_WINDOWS_COLOR_SPACE)

    // Pixel data sits at the same offset the existing committed assets use,
    // so files written here stay byte-comparable with them.
    private const int PixelDataOffset = 150;

    public static void Write(FastBitmap bitmap, string path)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        int width = bitmap.Width;
        int height = bitmap.Height;
        int dataSize = width * height * 4;
        const int pixelOffset = PixelDataOffset;

        using BinaryWriter writer = new(File.Create(path));

        // BITMAPFILEHEADER
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(pixelOffset + dataSize);
        writer.Write(0);
        writer.Write(pixelOffset);

        // BITMAPV5HEADER
        writer.Write(DibHeaderSize);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1); // planes
        writer.Write((short)32); // bits per pixel
        writer.Write(3); // BI_BITFIELDS
        writer.Write(dataSize);
        writer.Write(2835); // x pixels per metre (72 dpi)
        writer.Write(2835); // y pixels per metre
        writer.Write(0); // colours used
        writer.Write(0); // colours important
        writer.Write(0x00FF0000); // red mask
        writer.Write(0x0000FF00); // green mask
        writer.Write(0x000000FF); // blue mask
        writer.Write(0xFF000000); // alpha mask
        writer.Write(WindowsColorSpace);
        writer.Write(new byte[36]); // CIEXYZTRIPLE endpoints, unused for "Win "
        writer.Write(0); // gamma red
        writer.Write(0); // gamma green
        writer.Write(0); // gamma blue
        writer.Write(0); // intent
        writer.Write(0); // profile data
        writer.Write(0); // profile size
        writer.Write(0); // reserved

        // pad up to BitmapReader's fixed pixel-data offset
        writer.Write(new byte[pixelOffset - FileHeaderSize - DibHeaderSize]);

        // pixel data: bottom-up rows of BGRA
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                FastColor colour = bitmap.GetPixel(x, y);
                writer.Write(colour.B);
                writer.Write(colour.G);
                writer.Write(colour.R);
                writer.Write(colour.A);
            }
        }
    }
}

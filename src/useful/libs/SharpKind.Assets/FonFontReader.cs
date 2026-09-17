// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Buffers.Binary;

namespace SharpKind.Assets;

/// <summary>
/// Reads Windows .fon bitmap fonts. A .fon is a 16-bit New Executable whose
/// resources are FNT strikes, so the file is opened as an executable, its
/// resource table walked for the font resources, and each of those decoded as
/// a strike. Both FNT versions that carry a character table are handled -
/// 2.0 and 3.0 - fixed pitch and proportional alike.
/// </summary>
/// <remarks>
/// A bitmap face has only the sizes it was drawn at, so <see cref="Read(string, int)"/>
/// takes the strike nearest the height asked for rather than scaling one:
/// pixel-doubling a 10px face to 12px would show neither size honestly.
/// </remarks>
public static class FonFontReader
{
    // Where the NE header's offset sits in the DOS stub that precedes it.
    private const int NewExeOffsetPosition = 0x3C;

    // Offset of the resource table, from the NE header.
    private const int ResourceTablePosition = 0x24;

    // Resource type id, as the resource table numbers them. The high bit
    // marks an integer id rather than a name, and 0x08 is RT_FONT.
    private const ushort FontResourceType = 0x8008;

    // Common to both versions - 3.0 only added fields after them, which is why the character table moves and these don't.
    private const int PixelHeightPosition = 0x58;
    private const int PitchAndFamilyPosition = 0x5A;
    private const int MaxWidthPosition = 0x5D;
    private const int FirstCharPosition = 0x5F;
    private const int LastCharPosition = 0x60;

    private const ushort Fnt2 = 0x0200;
    private const ushort Fnt3 = 0x0300;

    /// <summary>
    /// Reads the strike nearest <paramref name="pixelHeight"/> from a .fon file.
    /// </summary>
    /// <param name="path">Path to the .fon file.</param>
    /// <param name="pixelHeight">The pixel height wanted; must be positive.</param>
    /// <returns>The nearest strike the file holds.</returns>
    /// <exception cref="SharpKindException">
    /// <paramref name="pixelHeight"/> is not a real size, or the file is not a
    /// readable .fon. Nearest-strike would answer for any number, so a height
    /// left unsaid - which binds as zero - would quietly take the smallest
    /// strike in the file rather than saying no size was ever named.
    /// </exception>
    public static FonFont Read(string path, int pixelHeight)
        => pixelHeight <= 0
            ? throw new SharpKindException(
                $"'{path}' was asked for a strike {pixelHeight}px high; a font has to be asked for a real size.")
            : Read(File.ReadAllBytes(path), path, pixelHeight);

    /// <summary>
    /// The pixel heights of every strike in a .fon file, in the order the file
    /// holds them. Lets a caller report what a font offers when the height it
    /// wanted is not among them.
    /// </summary>
    /// <param name="path">Path to the .fon file.</param>
    /// <returns>Each strike's pixel height.</returns>
    public static IReadOnlyList<int> StrikeHeights(string path)
    {
        byte[] file = File.ReadAllBytes(path);

        return [.. FontResources(file, path).Select(x => ReadUInt16(file, x + PixelHeightPosition, path))];
    }

    internal static FonFont Read(byte[] file, string path, int pixelHeight)
    {
        int[] resources = [.. FontResources(file, path)];

        if (resources.Length == 0)
        {
            throw new SharpKindException($"'{path}' holds no font resources, so it is not a usable .fon file.");
        }

        // Nearest strike, ties going to the smaller: text a pixel short of
        // its box still fits, text a pixel over does not.
        int nearest = resources
            .OrderBy(x => Math.Abs(ReadUInt16(file, x + PixelHeightPosition, path) - pixelHeight))
            .ThenBy(x => ReadUInt16(file, x + PixelHeightPosition, path))
            .First();

        return ReadStrike(file, nearest, path);
    }

    // Every RT_FONT resource's offset. Offsets are stored shifted by the table's leading count so a 16-bit field can address a larger file.
    private static List<int> FontResources(byte[] file, string path)
    {
        if (ReadUInt16(file, 0, path) != 0x5A4D)
        {
            throw new SharpKindException($"'{path}' is not an executable, so it cannot be a .fon file.");
        }

        int header = (int)ReadUInt32(file, NewExeOffsetPosition, path);

        if (ReadUInt16(file, header, path) != 0x454E)
        {
            throw new SharpKindException(
                $"'{path}' is not a 16-bit New Executable, which is the only form a .fon file takes.");
        }

        int table = header + ReadUInt16(file, header + ResourceTablePosition, path);
        int shift = ReadUInt16(file, table, path);
        int position = table + 2;
        List<int> resources = [];

        // A type id of zero ends the table.
        for (ushort type = ReadUInt16(file, position, path); type != 0; type = ReadUInt16(file, position, path))
        {
            int count = ReadUInt16(file, position + 2, path);

            // Type id, count, and four reserved bytes, then the resources.
            position += 8;

            for (int i = 0; i < count; i++)
            {
                if (type == FontResourceType)
                {
                    resources.Add(ReadUInt16(file, position, path) << shift);
                }

                // Offset, length, flags, id, and four reserved bytes.
                position += 12;
            }
        }

        return resources;
    }

    private static FonFont ReadStrike(byte[] file, int start, string path)
    {
        ushort version = ReadUInt16(file, start, path);

        if (version is not (Fnt2 or Fnt3))
        {
            throw new SharpKindException(
                $"'{path}' holds a version {version >> 8}.{version & 0xFF} font; only 2.0 and 3.0 carry a character table.");
        }

        int height = ReadUInt16(file, start + PixelHeightPosition, path);
        int maxWidth = ReadUInt16(file, start + MaxWidthPosition, path);

        // Bit 0 of dfPitchAndFamily: set means the glyphs vary in width.
        bool isProportional = (ReadByte(file, start + PitchAndFamilyPosition, path) & 0x01) != 0;
        char firstChar = (char)ReadByte(file, start + FirstCharPosition, path);
        char lastChar = (char)ReadByte(file, start + LastCharPosition, path);

        if (lastChar < firstChar)
        {
            throw new SharpKindException($"'{path}' declares a font whose last character precedes its first.");
        }

        // 3.0 widened the glyph offset from two bytes to four, which moved
        // the character table down by the fields inserted ahead of it.
        int table = start + (version == Fnt2 ? 0x76 : 0x94);
        int entry = version == Fnt2 ? 4 : 6;

        FonGlyph[] glyphs = new FonGlyph[lastChar - firstChar + 1];

        for (int i = 0; i < glyphs.Length; i++)
        {
            int width = ReadUInt16(file, table + (i * entry), path);
            int offset = version == Fnt2
                ? ReadUInt16(file, table + (i * entry) + 2, path)
                : (int)ReadUInt32(file, table + (i * entry) + 2, path);

            glyphs[i] = ReadGlyph(file, start + offset, width, height, path);
        }

        return new(height, maxWidth, firstChar, lastChar, isProportional, glyphs);
    }

    // Glyph is stored as columns of bytes (leftmost 8px column-major, top to bottom, then next 8...) - the packing a 16-bit display adapter wanted.
    private static FonGlyph ReadGlyph(byte[] file, int start, int width, int height, string path)
    {
        bool[] ink = new bool[width * height];
        int columns = (width + 7) / 8;

        for (int column = 0; column < columns; column++)
        {
            for (int y = 0; y < height; y++)
            {
                byte bits = ReadByte(file, start + (column * height) + y, path);

                for (int bit = 0; bit < 8; bit++)
                {
                    int x = (column * 8) + bit;

                    if (x < width)
                    {
                        ink[(y * width) + x] = (bits & (0x80 >> bit)) != 0;
                    }
                }
            }
        }

        return new(width, height, ink);
    }

    // Every field is read through these so a truncated file fails naming the file, not with a bare index error.
    private static byte ReadByte(byte[] file, int position, string path)
    {
        Require(file, position, sizeof(byte), path);

        return file[position];
    }

    private static ushort ReadUInt16(byte[] file, int position, string path)
    {
        Require(file, position, sizeof(ushort), path);

        return BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(position));
    }

    private static uint ReadUInt32(byte[] file, int position, string path)
    {
        Require(file, position, sizeof(uint), path);

        return BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(position));
    }

    private static void Require(byte[] file, int position, int length, string path)
    {
        if (position < 0 || position + length > file.Length)
        {
            throw new SharpKindException($"'{path}' ends part way through a font it declares, so it cannot be read.");
        }
    }
}

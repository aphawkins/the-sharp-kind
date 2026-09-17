// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Buffers.Binary;

namespace SharpKind.Fakes.Assets;

// Builds a .fon in memory, filling in only the parts FonFontReader reads - enough to be a font, not enough to be a loadable executable.
public static class FonFileBuilder
{
    // Resource offsets and lengths are stored shifted by this, so every
    // resource has to start on a multiple of it.
    private const int Shift = 4;
    private const int Alignment = 1 << Shift;

    private const int NewExeOffsetPosition = 0x3C;
    private const int ResourceTableOffsetPosition = 0x24;

    private const int StubLength = 0x40;
    private const int HeaderLength = 0x40;

    public static byte[] Build(params FontSpec[] fonts)
    {
        byte[][] resources = [.. fonts.Select(Strike)];

        // Entries need the resources' offsets, which depend on where the table ends, so its length is worked out first.
        int tableLength = 2 + 8 + (12 * resources.Length) + 2;
        const int table = StubLength + HeaderLength;
        int position = Align(table + tableLength);

        List<byte> file = [.. new byte[position]];
        List<int> offsets = [];

        foreach (byte[] resource in resources)
        {
            offsets.Add(position);
            file.AddRange(resource);
            file.AddRange(new byte[Align(file.Count) - file.Count]);
            position = file.Count;
        }

        byte[] bytes = [.. file];

        // DOS stub, holding only the signature and where the real header is.
        WriteUInt16(bytes, 0, 0x5A4D);
        WriteUInt32(bytes, NewExeOffsetPosition, StubLength);

        // NE header, holding only the signature and where its resources are.
        WriteUInt16(bytes, StubLength, 0x454E);
        WriteUInt16(bytes, StubLength + ResourceTableOffsetPosition, table - StubLength);

        WriteUInt16(bytes, table, Shift);
        WriteUInt16(bytes, table + 2, 0x8008);
        WriteUInt16(bytes, table + 4, (ushort)resources.Length);

        for (int i = 0; i < resources.Length; i++)
        {
            int entry = table + 2 + 8 + (12 * i);
            WriteUInt16(bytes, entry, (ushort)(offsets[i] >> Shift));
            WriteUInt16(bytes, entry + 2, (ushort)(Align(resources[i].Length) >> Shift));
        }

        return bytes;
    }

    // One FNT resource: the header fields the reader looks at, a character
    // table, and the glyph bits the table points into.
    private static byte[] Strike(FontSpec font)
    {
        int tablePosition = font.Version == 0x0200 ? 0x76 : 0x94;
        int entryLength = font.Version == 0x0200 ? 4 : 6;
        int count = font.LastChar - font.FirstChar + 1;
        int bits = tablePosition + (entryLength * count);

        List<byte> glyphs = [];
        List<(int Width, int Offset)> entries = [];

        foreach (string[] glyph in font.Glyphs)
        {
            entries.Add((glyph[0].Length, bits + glyphs.Count));
            glyphs.AddRange(Pack(glyph));
        }

        byte[] resource = new byte[bits + glyphs.Count];
        glyphs.CopyTo(resource, bits);

        WriteUInt16(resource, 0, font.Version);
        WriteUInt16(resource, 0x58, (ushort)font.PixelHeight);
        resource[0x5A] = (byte)(font.IsProportional ? 0x01 : 0x00);
        WriteUInt16(resource, 0x5D, (ushort)entries.Max(x => x.Width));
        resource[0x5F] = (byte)font.FirstChar;
        resource[0x60] = (byte)font.LastChar;

        for (int i = 0; i < entries.Count; i++)
        {
            int entry = tablePosition + (entryLength * i);
            WriteUInt16(resource, entry, (ushort)entries[i].Width);

            if (font.Version == 0x0200)
            {
                WriteUInt16(resource, entry + 2, (ushort)entries[i].Offset);
            }
            else
            {
                WriteUInt32(resource, entry + 2, (uint)entries[i].Offset);
            }
        }

        return resource;
    }

    // Ink into the format's column-major packing: all of the leftmost eight
    // pixel columns first, top to bottom, then the next eight.
    private static byte[] Pack(string[] glyph)
    {
        int width = glyph[0].Length;
        int height = glyph.Length;
        int columns = (width + 7) / 8;
        byte[] packed = new byte[columns * height];

        for (int column = 0; column < columns; column++)
        {
            for (int y = 0; y < height; y++)
            {
                byte value = 0;

                for (int bit = 0; bit < 8; bit++)
                {
                    int x = (column * 8) + bit;

                    if (x < width && glyph[y][x] == '#')
                    {
                        value |= (byte)(0x80 >> bit);
                    }
                }

                packed[(column * height) + y] = value;
            }
        }

        return packed;
    }

    private static int Align(int position) => (position + Alignment - 1) / Alignment * Alignment;

    private static void WriteUInt16(byte[] bytes, int position, ushort value)
        => BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(position), value);

    private static void WriteUInt32(byte[] bytes, int position, uint value)
        => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(position), value);
}

// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Emulation.Sid;

/// <summary>A PSID tune: the header fields and the program to load. RSID and CIA-timed tunes are not supported.</summary>
public sealed class PsidFile
{
    private const int MinimumHeader = 0x76;
    private const int NameLength = 32;

    private PsidFile(byte[] data) => Data = data;

    public string Name { get; private init; } = string.Empty;

    public string Author { get; private init; } = string.Empty;

    public string Released { get; private init; } = string.Empty;

    public int LoadAddress { get; private init; }

    public int InitAddress { get; private init; }

    public int PlayAddress { get; private init; }

    public int Songs { get; private init; }

    public int StartSong { get; private init; }

    /// <summary>Gets the program bytes, to be loaded at <see cref="LoadAddress"/>.</summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>Reads a PSID file.</summary>
    /// <param name="bytes">The whole file.</param>
    /// <returns>The tune.</returns>
    /// <exception cref="SharpKindException">The file is not a PSID this player can play.</exception>
    public static PsidFile Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length < MinimumHeader)
        {
            throw new SharpKindException("The file is too short to be a PSID.");
        }

        string magic = Encoding.ASCII.GetString(bytes, 0, 4);

        if (magic != "PSID")
        {
            throw new SharpKindException($"The file is a '{magic}', not a PSID, which is not supported.");
        }

        int dataOffset = Word(bytes, 0x06);
        int load = Word(bytes, 0x08);
        int play = Word(bytes, 0x0C);
        int songs = Word(bytes, 0x0E);
        uint speed = (uint)((Word(bytes, 0x12) << 16) | Word(bytes, 0x14));

        if (play == 0)
        {
            throw new SharpKindException("The PSID has no play address, which is not supported.");
        }

        if (speed != 0)
        {
            throw new SharpKindException("The PSID asks for CIA timing, which is not supported.");
        }

        int start = Word(bytes, 0x10);

        if (songs < 1 || start < 1 || start > songs)
        {
            throw new SharpKindException($"The PSID has {songs} songs and starts at {start}.");
        }

        int from = dataOffset;

        if (load == 0)
        {
            load = bytes[from] | (bytes[from + 1] << 8);
            from += 2;
        }

        return new PsidFile(bytes[from..])
        {
            Name = Text(bytes, 0x16),
            Author = Text(bytes, 0x36),
            Released = Text(bytes, 0x56),
            LoadAddress = load,
            InitAddress = Word(bytes, 0x0A),
            PlayAddress = play,
            Songs = songs,
            StartSong = start,
        };
    }

    private static int Word(byte[] bytes, int offset) => (bytes[offset] << 8) | bytes[offset + 1];

    private static string Text(byte[] bytes, int offset)
    {
        string text = Encoding.Latin1.GetString(bytes, offset, NameLength);
        int end = text.IndexOf('\0', StringComparison.Ordinal);

        return end < 0 ? text : text[..end];
    }
}

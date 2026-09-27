// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>What was drawn in each object slot this pass.</summary>
public sealed class ObjectsModel
{
    private readonly byte[] _drawn;
    private readonly byte[] _column;
    private readonly byte[] _row;
    private readonly byte[] _subY;

    /// <summary>Initializes a new instance of the <see cref="ObjectsModel"/> class.</summary>
    /// <param name="drawn">Each slot's object-sprites.tga entry.</param>
    /// <param name="column">Each slot's character column.</param>
    /// <param name="row">Each slot's character row.</param>
    /// <param name="subY">Each slot's line within its character row.</param>
    /// <param name="colours">The level's colour byte.</param>
    public ObjectsModel(
        ReadOnlySpan<byte> drawn,
        ReadOnlySpan<byte> column,
        ReadOnlySpan<byte> row,
        ReadOnlySpan<byte> subY,
        int colours)
    {
        Check(drawn, nameof(drawn));
        Check(column, nameof(column));
        Check(row, nameof(row));
        Check(subY, nameof(subY));

        _drawn = drawn[..Capacity].ToArray();
        _column = column[..Capacity].ToArray();
        _row = row[..Capacity].ToArray();
        _subY = subY[..Capacity].ToArray();
        Colours = colours;
    }

    /// <summary>Gets how many object slots there are.</summary>
    public static int Capacity => 18;

    /// <summary>Gets the entry of a slot with nothing drawn.</summary>
    public static byte NotDrawn => 0xFF;

    /// <summary>Gets the level's colour byte.</summary>
    public int Colours { get; }

    /// <summary>Gets a value indicating whether a slot was drawn.</summary>
    /// <param name="slot">The object slot.</param>
    /// <returns>Whether the slot was drawn.</returns>
    public bool IsDrawn(int slot) => _drawn[slot] != NotDrawn;

    /// <summary>Gets a slot's object-sprites.tga entry.</summary>
    /// <param name="slot">The object slot.</param>
    /// <returns>The entry index.</returns>
    public byte Entry(int slot) => _drawn[slot];

    /// <summary>Gets a slot's character column.</summary>
    /// <param name="slot">The object slot.</param>
    /// <returns>The column.</returns>
    public byte Column(int slot) => _column[slot];

    /// <summary>Gets a slot's character row.</summary>
    /// <param name="slot">The object slot.</param>
    /// <returns>The row.</returns>
    public byte Row(int slot) => _row[slot];

    /// <summary>Gets a slot's line within its character row.</summary>
    /// <param name="slot">The object slot.</param>
    /// <returns>The sub-position byte.</returns>
    public byte SubY(int slot) => _subY[slot];

    private static void Check(ReadOnlySpan<byte> bytes, string name)
    {
        if (bytes.Length < Capacity)
        {
            throw new ArgumentException(
                $"There are {Capacity} object slots, and only {bytes.Length} bytes of '{name}'.",
                name);
        }
    }
}

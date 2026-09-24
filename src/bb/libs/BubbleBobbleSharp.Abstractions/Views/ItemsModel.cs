// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>
/// What the level's two items look like on screen: the bonus food and the
/// special item - see docs/bb-port-plan.md, item 2e.
/// <para>
/// $1844 draws each item that shows as a 2x2 block of characters at its level
/// cell, the art $2B31 copied in, with one colour RAM byte on all four cells.
/// An item that does not show draws nothing: $1578 has already put back what
/// the cell held.
/// </para>
/// </summary>
public sealed class ItemsModel
{
    private readonly byte[] _type;
    private readonly byte[] _column;
    private readonly byte[] _row;
    private readonly byte[] _art;
    private readonly byte[] _colour;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemsModel"/> class.
    /// </summary>
    /// <param name="type">Each item's type, as $52/$53 hold it. Bit 7 set does not show.</param>
    /// <param name="column">Each item's level column.</param>
    /// <param name="row">Each item's level row.</param>
    /// <param name="art">Each item's block of item art.</param>
    /// <param name="colour">Each item's colour RAM byte, as $5F/$60 hold it.</param>
    /// <param name="colours">The level's colour byte, as <see cref="PlayfieldModel.Colours"/> holds it.</param>
    public ItemsModel(
        ReadOnlySpan<byte> type,
        ReadOnlySpan<byte> column,
        ReadOnlySpan<byte> row,
        ReadOnlySpan<byte> art,
        ReadOnlySpan<byte> colour,
        int colours)
    {
        Check(type, nameof(type));
        Check(column, nameof(column));
        Check(row, nameof(row));
        Check(art, nameof(art));
        Check(colour, nameof(colour));

        _type = type[..Capacity].ToArray();
        _column = column[..Capacity].ToArray();
        _row = row[..Capacity].ToArray();
        _art = art[..Capacity].ToArray();
        _colour = colour[..Capacity].ToArray();
        Colours = colours;
    }

    /// <summary>Gets how many items a level has.</summary>
    public static int Capacity => 2;

    /// <summary>Gets the level's colour byte, painted the same as the playfield's.</summary>
    public int Colours { get; }

    /// <summary>Gets a value indicating whether an item shows.</summary>
    /// <param name="item">The item: 0 the food, 1 the special item.</param>
    /// <returns>Whether $1844 draws it.</returns>
    public bool Shows(int item) => (_type[item] & 0x80) == 0;

    /// <summary>Gets the level column of an item's top-left character.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The column.</returns>
    public byte Column(int item) => _column[item];

    /// <summary>Gets the level row of an item's top-left character.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The row.</returns>
    public byte Row(int item) => _row[item];

    /// <summary>Gets an item's block in item-chars.tga.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The block.</returns>
    public byte Art(int item) => _art[item];

    /// <summary>Gets an item's colour RAM byte.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The colour byte.</returns>
    public byte Colour(int item) => _colour[item];

    private static void Check(ReadOnlySpan<byte> bytes, string name)
    {
        if (bytes.Length < Capacity)
        {
            throw new ArgumentException(
                $"There are {Capacity} items, and only {bytes.Length} bytes of '{name}'.",
                name);
        }
    }
}

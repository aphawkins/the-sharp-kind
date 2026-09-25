// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>
/// What the eighteen object slots look like on screen: bubbles, the pop
/// animation, and nothing else yet - see docs/bb-port-plan.md, item 2c.
/// <para>
/// A slot's picture is not read off its type directly. <c>BubblePop</c>'s
/// <c>Drawn</c> already turns a slot's type into the entry object-sprites.tga
/// holds it at, or <see cref="NotDrawn"/> - because the screen shows what that
/// pass drew before the AI loop moves anything, and by the time a view runs a
/// slot's type may have moved on from what was actually drawn. This model
/// just carries that record, and the position to draw it at, out to a
/// rendition.
/// </para>
/// </summary>
public sealed class ObjectsModel
{
    private readonly byte[] _drawn;
    private readonly byte[] _column;
    private readonly byte[] _row;
    private readonly byte[] _subY;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectsModel"/> class.
    /// </summary>
    /// <param name="drawn">
    /// Each slot's object-sprites.tga entry, as <c>BubblePop.Drawn</c> holds
    /// it.
    /// </param>
    /// <param name="column">Each slot's character column, as $DC holds it.</param>
    /// <param name="row">Each slot's character row, as $EE holds it.</param>
    /// <param name="subY">Each slot's vertical sub-position, as $A9D6 holds it.</param>
    /// <param name="colours">The level's colour byte, as <see cref="PlayfieldModel.Colours"/> holds it.</param>
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

    /// <summary>Gets how many object slots there are - the same count <c>ObjectTable</c> holds.</summary>
    public static int Capacity => 18;

    /// <summary>Gets the value a slot with nothing to draw this pass carries.</summary>
    public static byte NotDrawn => 0xFF;

    /// <summary>Gets the level's colour byte, painted the same as the playfield's.</summary>
    public int Colours { get; }

    /// <summary>Gets a value indicating whether a slot has anything to draw this pass.</summary>
    /// <param name="slot">The object slot.</param>
    /// <returns>Whether the slot was drawn.</returns>
    public bool IsDrawn(int slot) => _drawn[slot] != NotDrawn;

    /// <summary>Gets a slot's entry into object-sprites.tga. Only meaningful when <see cref="IsDrawn"/> is true.</summary>
    /// <param name="slot">The object slot.</param>
    /// <returns>The entry index.</returns>
    public byte Entry(int slot) => _drawn[slot];

    /// <summary>Gets a slot's character column, as $DC holds it.</summary>
    /// <param name="slot">The object slot.</param>
    /// <returns>The column.</returns>
    public byte Column(int slot) => _column[slot];

    /// <summary>Gets a slot's character row, as $EE holds it.</summary>
    /// <param name="slot">The object slot.</param>
    /// <returns>The row.</returns>
    public byte Row(int slot) => _row[slot];

    /// <summary>Gets a slot's vertical sub-position, as $A9D6 holds it.</summary>
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

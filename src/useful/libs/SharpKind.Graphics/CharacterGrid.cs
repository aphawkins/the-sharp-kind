// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;

namespace SharpKind.Graphics;

/// <summary>
/// A lattice of fixed-size character cells over a screen, and the arithmetic
/// for turning a cell index into a position.
/// <para>
/// It belongs to the screen rather than to whatever draws on it: an 8-bit
/// machine's display is a grid of character cells, and where column twelve
/// falls is a fact about the display, not a decision a view makes. Holding it
/// here rather than on a base view means the things that are not views - a
/// list style, a layout calculation, a test - can ask without first having to
/// be one.
/// </para>
/// <para>
/// Not every screen has one. A tier with a proportional font lays out in
/// pixels and should hold no grid at all rather than an invented cell size.
/// </para>
/// </summary>
/// <param name="CellWidth">The width of one character cell in pixels.</param>
/// <param name="CellHeight">The height of one character cell in pixels.</param>
/// <param name="Origin">
/// Where cell (0, 0) starts. Usually the screen's own origin, but a grid laid
/// over part of a screen - a playfield inset into a border - starts where that
/// part does.
/// </param>
public readonly record struct CharacterGrid(float CellWidth, float CellHeight, Vector2 Origin)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterGrid"/> struct
    /// on the screen's own origin.
    /// </summary>
    /// <param name="cellWidth">The width of one character cell in pixels.</param>
    /// <param name="cellHeight">The height of one character cell in pixels.</param>
    public CharacterGrid(float cellWidth, float cellHeight)
        : this(cellWidth, cellHeight, Vector2.Zero)
    {
    }

    /// <summary>
    /// Gets the x of a column, 0 being the leftmost.
    /// </summary>
    /// <param name="column">The column index.</param>
    /// <returns>The x in pixels.</returns>
    public float Column(int column) => Origin.X + (column * CellWidth);

    /// <summary>
    /// Gets the y of a row, 0 being the topmost.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <returns>The y in pixels.</returns>
    public float Row(int row) => Origin.Y + (row * CellHeight);

    /// <summary>
    /// Gets the top-left corner of one cell.
    /// </summary>
    /// <param name="column">The column index.</param>
    /// <param name="row">The row index.</param>
    /// <returns>The cell's top-left corner in pixels.</returns>
    public Vector2 Cell(int column, int row) => new(Column(column), Row(row));

    /// <summary>
    /// Gets how many whole columns fit in a width.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <returns>The number of whole columns.</returns>
    public int ColumnsIn(float width) => (int)(width / CellWidth);

    /// <summary>
    /// Gets how many whole rows fit in a height.
    /// </summary>
    /// <param name="height">The height in pixels.</param>
    /// <returns>The number of whole rows.</returns>
    public int RowsIn(float height) => (int)(height / CellHeight);

    /// <summary>
    /// Advances a position to the next cell boundary, for text whose place is
    /// decided by something other than the grid. Rounding up rather than to
    /// the nearest keeps a label clear of whatever it names, which rounding
    /// down would let it overlap.
    /// </summary>
    /// <param name="position">The position to advance.</param>
    /// <returns>The position, on the next cell boundary.</returns>
    public Vector2 SnapUp(Vector2 position) => new(
        Origin.X + (MathF.Ceiling((position.X - Origin.X) / CellWidth) * CellWidth),
        Origin.Y + (MathF.Ceiling((position.Y - Origin.Y) / CellHeight) * CellHeight));
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The level as screen codes, 32 columns by 25 rows.</summary>
public sealed class PlayfieldModel
{
    private const int WipeFirstColumn = 2;
    private const int WipeLastColumn = 29;

    private readonly byte[] _characters;

    /// <summary>Initializes a new instance of the <see cref="PlayfieldModel"/> class.</summary>
    /// <param name="tile">The level's own tile, counted from zero.</param>
    /// <param name="colours">The level's colour byte - see <see cref="Colours"/>.</param>
    /// <param name="characters">The screen codes, row by row.</param>
    public PlayfieldModel(int tile, int colours, ReadOnlySpan<byte> characters)
    {
        if (characters.Length != Columns * Rows)
        {
            throw new ArgumentException(
                $"A playfield is {Columns * Rows} characters, not {characters.Length}.",
                nameof(characters));
        }

        Tile = tile;
        Colours = colours;
        _characters = characters.ToArray();
    }

    /// <summary>Gets the columns the level is drawn in.</summary>
    public static int Columns => 32;

    /// <summary>Gets the rows the level is drawn in.</summary>
    public static int Rows => 25;

    /// <summary>Gets the screen code of a cell with nothing in it.</summary>
    public static byte Space => 0x20;

    /// <summary>Gets the screen code the level's own tile is drawn with.</summary>
    public static byte LevelTile => 0x15;

    /// <summary>Gets the screen code of a solid block, which a wiped cell becomes (<c>$47F8</c>).</summary>
    public static byte Block => 0xFF;

    /// <summary>Gets the colour RAM of a wiped row.</summary>
    public static int WipedColourRam => 0x0B;

    /// <summary>Gets the first of the tile edge and shadow characters.</summary>
    public static byte FirstTileEdge => 0x0A;

    /// <summary>Gets how many of those there are.</summary>
    public static int TileEdgeCount => 6;

    /// <summary>Gets which of the hundred tiles is this level's, counted from zero.</summary>
    public int Tile { get; }

    /// <summary>Gets the level's colour byte.</summary>
    public int Colours { get; }

    /// <summary>Gets how many rows down the level is drawn, negative being up. Past its top and bottom its edge rows go on.</summary>
    public int Shift { get; private init; }

    /// <summary>Gets the first screen row the level is drawn on.</summary>
    public int FirstRow { get; private init; }

    /// <summary>Gets the screen row the level is drawn down to, not including it.</summary>
    public int EndRow { get; private init; } = Rows;

    /// <summary>Gets how many rows, from the bottom, the special item of type 8 has wiped (<c>$2E15</c>).</summary>
    public int WipedRows { get; private init; }

    /// <summary>Gets the same level in other colours.</summary>
    /// <param name="colours">The level's colour byte.</param>
    /// <returns>The new model.</returns>
    public PlayfieldModel WithColours(int colours) => new(Tile, colours, _characters) { WipedRows = WipedRows };

    /// <summary>
    /// Gets the same level with its bottom rows wiped: in the 28 cells inside the walls everything but the level's tile
    /// (<c>$10</c> to <c>$1E</c>) is a solid block, and the colour RAM is <see cref="WipedColourRam"/>.
    /// </summary>
    /// <param name="rows">How many rows, from the bottom.</param>
    /// <returns>The new model.</returns>
    public PlayfieldModel Wiped(int rows) => new(Tile, Colours, _characters) { WipedRows = rows };

    /// <summary>Whether a row of the level is wiped.</summary>
    /// <param name="row">The row, 0 being the topmost.</param>
    /// <returns>Whether it is.</returns>
    public bool IsWiped(int row) => row >= Rows - WipedRows;

    /// <summary>Whether a cell of the level is wiped: one of the 28 inside the walls, on a wiped row.</summary>
    /// <param name="column">The column, 0 being the level's leftmost.</param>
    /// <param name="row">The row, 0 being the topmost.</param>
    /// <returns>Whether it is.</returns>
    public bool IsWiped(int column, int row) => IsWiped(row) && column is >= WipeFirstColumn and <= WipeLastColumn;

    /// <summary>Gets the same level moved up or down the screen and drawn on some of its rows, as the scroll to the next level does.</summary>
    /// <param name="shift">How many rows down the level is drawn, negative being up.</param>
    /// <param name="firstRow">The first screen row it is drawn on.</param>
    /// <param name="endRow">The screen row it is drawn down to, not including it.</param>
    /// <returns>The new model.</returns>
    public PlayfieldModel Scrolled(int shift, int firstRow, int endRow)
        => new(Tile, Colours, _characters) { Shift = shift, FirstRow = firstRow, EndRow = endRow, WipedRows = WipedRows };

    /// <summary>Gets the row of the level drawn on a screen row: past its edges, the edge row.</summary>
    /// <param name="screenRow">The screen row.</param>
    /// <returns>The level's row.</returns>
    public int SourceRow(int screenRow) => Math.Clamp(screenRow - Shift, 0, Rows - 1);

    /// <summary>Gets the screen code at one cell of the level.</summary>
    /// <param name="column">The column, 0 being the level's leftmost.</param>
    /// <param name="row">The row, 0 being the topmost.</param>
    /// <returns>The screen code.</returns>
    public byte Character(int column, int row)
    {
        byte character = _characters[(row * Columns) + column];

        return IsWiped(column, row) && character is not (>= 0x10 and <= 0x1E)
            ? Block
            : character;
    }
}

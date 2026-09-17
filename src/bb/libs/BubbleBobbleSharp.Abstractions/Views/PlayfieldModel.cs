// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>
/// The level as characters: thirty-two columns by twenty-five rows of screen
/// codes, which is what setup_level_screen leaves in screen memory.
/// <para>
/// The codes are the C64's own rather than something friendlier, because the
/// renderer's decisions are made in them - which character goes to the right
/// of a tile depends on which character is there already - and a view that
/// was handed anything else would be reading a translation of a translation.
/// Only three of them need naming to draw the grid, and they are named here.
/// </para>
/// </summary>
public sealed class PlayfieldModel
{
    private readonly byte[] _characters;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayfieldModel"/> class.
    /// </summary>
    /// <param name="tile">The level's own tile, counted from zero.</param>
    /// <param name="colours">The level's colour byte - see <see cref="Colours"/>.</param>
    /// <param name="characters">
    /// The screen codes, row by row: <see cref="Rows"/> rows of
    /// <see cref="Columns"/>.
    /// </param>
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

    /// <summary>
    /// Gets the screen code the level's own tile is drawn with. The character
    /// itself is the level's: setup_level_screen copies eight bytes out of the
    /// tile sheet into this one slot of the charset before the level is drawn.
    /// </summary>
    public static byte LevelTile => 0x15;

    /// <summary>
    /// Gets the first of the characters the renderer draws a tile's edges and
    /// its shadow with, the rest following it in order.
    /// </summary>
    public static byte FirstTileEdge => 0x0A;

    /// <summary>Gets how many of those there are.</summary>
    public static int TileEdgeCount => 6;

    /// <summary>
    /// Gets which of the hundred tiles is this level's, counted from zero.
    /// </summary>
    public int Tile { get; }

    /// <summary>
    /// Gets the level's colour byte, as levels.txt spells it: two nibbles,
    /// each naming one of the sixteen colours the playfield's artwork is
    /// painted in. The characters are two-bit entry numbers rather than
    /// colours, and this is what the entries point at - so a rendition reads
    /// it to know what the level actually looks like.
    /// </summary>
    public int Colours { get; }

    /// <summary>
    /// Gets the screen code at one cell of the level.
    /// </summary>
    /// <param name="column">The column, 0 being the level's leftmost.</param>
    /// <param name="row">The row, 0 being the topmost.</param>
    /// <returns>The screen code.</returns>
    public byte Character(int column, int row) => _characters[(row * Columns) + column];
}

// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The characters the water, the fire and the giant bubble draw into the level.</summary>
public sealed class EffectsModel
{
    // `$AD1E`'s row 4 is the screen's first, so a water row is four more than the screen row it is drawn on.
    private const int WaterFirstRow = 4;

    private readonly byte[] _waterColumns;
    private readonly byte[] _waterRows;
    private readonly byte[] _fireColumns;
    private readonly byte[] _fireRows;
    private readonly byte[] _fireLengths;
    private readonly byte[] _fireCharacters;

    /// <summary>Initializes a new instance of the <see cref="EffectsModel"/> class.</summary>
    /// <param name="waterRunning">Whether there is water.</param>
    /// <param name="waterColumns">The stream's eleven columns, the head last.</param>
    /// <param name="waterRows">The stream's eleven rows.</param>
    /// <param name="fireColumns">Each strip's first column.</param>
    /// <param name="fireRows">Each strip's row.</param>
    /// <param name="fireLengths">Each strip's length in cells.</param>
    /// <param name="fireCharacters">The screen code each strip is drawn with; 0 draws nothing.</param>
    /// <param name="colours">The level's colour byte.</param>
    public EffectsModel(
        bool waterRunning,
        ReadOnlySpan<byte> waterColumns,
        ReadOnlySpan<byte> waterRows,
        ReadOnlySpan<byte> fireColumns,
        ReadOnlySpan<byte> fireRows,
        ReadOnlySpan<byte> fireLengths,
        ReadOnlySpan<byte> fireCharacters,
        int colours)
    {
        WaterRunning = waterRunning;
        _waterColumns = waterColumns[..WaterCapacity].ToArray();
        _waterRows = waterRows[..WaterCapacity].ToArray();
        _fireColumns = fireColumns[..FireCapacity].ToArray();
        _fireRows = fireRows[..FireCapacity].ToArray();
        _fireLengths = fireLengths[..FireCapacity].ToArray();
        _fireCharacters = fireCharacters[..FireCapacity].ToArray();
        Colours = colours;
    }

    /// <summary>Gets the screen code of a water cell.</summary>
    public static byte WaterCharacter => 0x1F;

    /// <summary>Gets how many cells the water's stream has.</summary>
    public static int WaterCapacity => 11;

    /// <summary>Gets how many strips of fire there can be.</summary>
    public static int FireCapacity => 5;

    /// <summary>Gets a value indicating whether there is water.</summary>
    public bool WaterRunning { get; }

    /// <summary>Gets the level's colour byte, painted the same as the playfield's.</summary>
    public int Colours { get; }

    /// <summary>Gets a value indicating whether the giant bubble of the special item of type 7 is sweeping the level.</summary>
    public bool ShowsGiant { get; init; }

    /// <summary>Gets the screen column of the giant bubble's top-left cell, of its four by four.</summary>
    public int GiantColumn { get; init; }

    /// <summary>Gets the screen row of the giant bubble's top-left cell.</summary>
    public int GiantRow { get; init; }

    /// <summary>Gets a value indicating whether a water cell is drawn: not the tail, and only within the level.</summary>
    /// <param name="cell">The cell, 0 the tail to 10 the head.</param>
    /// <returns>Whether it is drawn.</returns>
    public bool ShowsWater(int cell) => WaterRunning && cell > 0 && _waterRows[cell] is >= WaterFirstRow and < 0x1D;

    /// <summary>Gets a water cell's level column.</summary>
    /// <param name="cell">The cell.</param>
    /// <returns>The column.</returns>
    public byte WaterColumn(int cell) => _waterColumns[cell];

    /// <summary>Gets a water cell's screen row.</summary>
    /// <param name="cell">The cell.</param>
    /// <returns>The row.</returns>
    public int WaterRow(int cell) => _waterRows[cell] - WaterFirstRow;

    /// <summary>Gets the screen code a strip is drawn with, which is 0 when it is not burning.</summary>
    /// <param name="strip">The strip.</param>
    /// <returns>The screen code.</returns>
    public byte StripCharacter(int strip) => _fireCharacters[strip];

    /// <summary>Gets a strip's first column.</summary>
    /// <param name="strip">The strip.</param>
    /// <returns>The column.</returns>
    public byte StripColumn(int strip) => _fireColumns[strip];

    /// <summary>Gets a strip's row.</summary>
    /// <param name="strip">The strip.</param>
    /// <returns>The row.</returns>
    public byte StripRow(int strip) => _fireRows[strip];

    /// <summary>Gets a strip's length in cells.</summary>
    /// <param name="strip">The strip.</param>
    /// <returns>The length.</returns>
    public byte StripLength(int strip) => _fireLengths[strip];
}

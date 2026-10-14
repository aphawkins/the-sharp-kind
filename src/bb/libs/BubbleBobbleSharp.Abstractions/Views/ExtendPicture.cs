// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>What the EXTEND bonus (<c>$32CD</c>) shows in place of the level.</summary>
public sealed class ExtendPicture
{
    private readonly byte[] _art;
    private readonly byte[] _colourRam;

    /// <summary>Initializes a new instance of the <see cref="ExtendPicture"/> class.</summary>
    /// <param name="screen">The screen shown.</param>
    /// <param name="player">The player who has the letters, from zero.</param>
    /// <param name="tile">The level's own tile, counted from zero, which the box is drawn in.</param>
    /// <param name="colours">The level's colour byte.</param>
    /// <param name="art">The block of item art of each fruit, row by row.</param>
    /// <param name="colourRam">The colour RAM byte of each fruit, row by row.</param>
    public ExtendPicture(ExtendScreen screen, int player, int tile, int colours, ReadOnlySpan<byte> art, ReadOnlySpan<byte> colourRam)
    {
        if (art.Length != FruitColumns * FruitRows || colourRam.Length != art.Length)
        {
            throw new ArgumentException($"The screen of fruit is {FruitColumns * FruitRows} blocks.", nameof(art));
        }

        Screen = screen;
        Player = player;
        Tile = tile;
        Colours = colours;
        _art = art.ToArray();
        _colourRam = colourRam.ToArray();
    }

    /// <summary>Gets the columns of 2x2 fruit blocks, from column 0.</summary>
    public static int FruitColumns => 16;

    /// <summary>Gets the rows of 2x2 fruit blocks, from row 0.</summary>
    public static int FruitRows => 12;

    /// <summary>Gets the box's left wall's column (<c>$312E</c>).</summary>
    public static int BoxLeft => 6;

    /// <summary>Gets the box's right wall's column.</summary>
    public static int BoxRight => 25;

    /// <summary>Gets the box's top wall's row.</summary>
    public static int BoxTop => 5;

    /// <summary>Gets the box's bottom wall's row.</summary>
    public static int BoxBottom => 14;

    /// <summary>Gets the column of the player's line (<c>$A7F9</c>).</summary>
    public static int NameColumn => 12;

    /// <summary>Gets the row of the player's line.</summary>
    public static int NameRow => 7;

    /// <summary>Gets the column of the line under it.</summary>
    public static int GetsColumn => 14;

    /// <summary>Gets the row of the line under it.</summary>
    public static int GetsRow => 9;

    /// <summary>Gets the colour RAM byte each turn of the screens (<c>$1853</c>) puts in the top two cells of the first fruit.</summary>
    public static int CornerColourRam => 0x09;

    /// <summary>Gets the text's colour RAM byte: under 8, a hires character in that colour.</summary>
    public static int TextColourRam => 0x07;

    /// <summary>Gets the column of the EXTEND banner's first letter (<c>$33DA</c>).</summary>
    public static int BannerColumn => 10;

    /// <summary>Gets the row of the EXTEND banner's top.</summary>
    public static int BannerRow => 11;

    /// <summary>Gets the EXTEND banner's letters, as the sidebar's letter pictures (<c>$A80D</c>).</summary>
    public static IReadOnlyList<int> Banner { get; } = [0, 1, 2, 0, 3, 4];

    /// <summary>Gets the line under it.</summary>
    public static string GetsText => "GETS";

    /// <summary>Gets the screen shown.</summary>
    public ExtendScreen Screen { get; }

    /// <summary>Gets the player who has the letters, from zero.</summary>
    public int Player { get; }

    /// <summary>Gets the level's own tile, counted from zero.</summary>
    public int Tile { get; }

    /// <summary>Gets the level's colour byte.</summary>
    public int Colours { get; }

    /// <summary>Gets the player's line.</summary>
    public string NameText => $"PLAYER {Player + 1}";

    /// <summary>Whether a cell is inside the box's walls.</summary>
    /// <param name="column">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>Whether it is.</returns>
    public static bool IsInside(int column, int row) => column is > 6 and < 25 && row is > 5 and < 14;

    /// <summary>Gets the block of item art of a fruit.</summary>
    /// <param name="column">The block's column, from 0 to <see cref="FruitColumns"/>.</param>
    /// <param name="row">The block's row, from 0 to <see cref="FruitRows"/>.</param>
    /// <returns>The block.</returns>
    public int Art(int column, int row) => _art[(row * FruitColumns) + column];

    /// <summary>Gets the colour RAM byte of a fruit.</summary>
    /// <param name="column">The block's column.</param>
    /// <param name="row">The block's row.</param>
    /// <returns>The colour RAM byte.</returns>
    public int ColourRam(int column, int row) => _colourRam[(row * FruitColumns) + column];
}

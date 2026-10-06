// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The decoration down both edges of the level.</summary>
/// <param name="Design">The level's design, or <see cref="NoDesign"/>.</param>
/// <param name="HeaderTile">The level's header tile, used when it has no design.</param>
/// <param name="Colours">The level's colour byte.</param>
/// <param name="Number">The level's number, drawn in the top-left corner; 0 draws none.</param>
public sealed record SidebarModel(int Design, int HeaderTile, int Colours, int Number = 0)
{
    // $38E3: level 100 has no room for three digits, so it is drawn as two brackets (glyphs $0A and $0B).
    private const int LastLevel = 100;

    // $AD15, as far as $ACFD shows it: READY, a blank and the HUD font's double exclamation mark, which is its '!'.
    private const string Ready = "READY !";

    /// <summary>Gets the design of a level that has none.</summary>
    public static int NoDesign => -1;

    /// <summary>Gets how many designs the sidebar sheet holds.</summary>
    public static int DesignCount => 59;

    /// <summary>Gets how many characters one design is made of.</summary>
    public static int CharactersPerDesign => 4;

    /// <summary>Gets how many rows down the decoration is drawn, negative being up; its pattern goes on past the level.</summary>
    public int Shift { get; init; }

    /// <summary>Gets the first screen row the decoration is drawn on.</summary>
    public int FirstRow { get; init; }

    /// <summary>Gets the screen row the decoration is drawn down to, not including it.</summary>
    public int EndRow { get; init; } = PlayfieldModel.Rows;

    /// <summary>Gets the round the ROUND and READY!! text announces over the level, or 0 while it is not shown.</summary>
    public int Round { get; init; }

    /// <summary>Gets player one's EXTEND letters ($54), one bit a letter from E in bit 0; none while they are not shown.</summary>
    public int PlayerOneLetters { get; init; }

    /// <summary>Gets player two's EXTEND letters ($55), one bit a letter from E in bit 0; none while they are not shown.</summary>
    public int PlayerTwoLetters { get; init; }

    /// <summary>Gets how many letters EXTEND is.</summary>
    public static int LetterCount => 6;

    /// <summary>Gets the screen row the first letter is drawn on; the others are two rows apart below it ($E6CD).</summary>
    public static int FirstLetterRow => 10;

    /// <summary>Gets the ROUND line ($A9A8-$A9B0), or an empty string while the text is not shown.</summary>
    /// <remarks>$3C01: a zero tens digit is blank, and level 100 is the three digits, with no blank after them.</remarks>
    public string RoundText => Round switch
    {
        <= 0 => string.Empty,
        LastLevel => "ROUND 100",
        < 10 => $"ROUND  {Round} ",
        _ => $"ROUND {Round} ",
    };

    /// <summary>Gets the READY!! line, or an empty string while the text is not shown.</summary>
    public string ReadyText => Round <= 0 ? string.Empty : Ready;

    /// <summary>Gets a value indicating whether the level wears one of the designs.</summary>
    public bool HasDesign => Design != NoDesign;

    /// <summary>Gets the two characters the level number is drawn as, or an empty string when none is drawn.</summary>
    /// <remarks>A zero tens digit is left blank ($38AD).</remarks>
    public string NumberText
    {
        get
        {
            if (Number <= 0)
            {
                return string.Empty;
            }

            if (Number == LastLevel)
            {
                return "[]";
            }

            char tens = Number < 10 ? ' ' : (char)('0' + (Number / 10));

            return $"{tens}{Number % 10}";
        }
    }

    /// <summary>Which of the five letter pictures a letter of EXTEND is ($E73A): the two E's share one.</summary>
    /// <param name="letter">The letter, 0 the first E.</param>
    /// <returns>The picture: E, X, T, N, D.</returns>
    public static int LetterPicture(int letter) => letter switch
    {
        0 or 3 => 0,
        1 or 2 => letter,
        _ => letter - 1,
    };
}

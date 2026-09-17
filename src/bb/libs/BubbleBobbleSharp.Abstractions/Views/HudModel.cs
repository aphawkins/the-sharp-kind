// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>
/// What the eight columns beside the level hold: both players' scores, the
/// high score, and how many lives each player has left.
/// <para>
/// A score is three bytes of packed BCD, which is six digits, because that is
/// how the game keeps one: add_score at $7C24 adds in decimal mode and carries
/// from one byte to the next. The model takes those bytes as they stand rather
/// than a number, so the arithmetic Phase 7 translates stays the arithmetic the
/// reference does.
/// </para>
/// <para>
/// The digits arrive already blanked. display_score_digits at $E3D9 walks the
/// six and hands each to $3FB0, which writes a space instead of a leading zero
/// until it has seen a digit that is not one - and always writes the last digit,
/// so a score of nothing reads as a single zero rather than an empty row. That
/// is a rule about the score rather than about how a rendition draws it, so it
/// is settled here.
/// </para>
/// </summary>
public sealed class HudModel
{
    private const char Blank = ' ';

    /// <summary>
    /// Initializes a new instance of the <see cref="HudModel"/> class.
    /// </summary>
    /// <param name="playerOneScore">Player one's score, as packed BCD at $0400.</param>
    /// <param name="playerTwoScore">Player two's score, as packed BCD at $0403.</param>
    /// <param name="highScore">The high score, as packed BCD at $0406.</param>
    /// <param name="playerOneLives">Player one's lives, as $045A holds them.</param>
    /// <param name="playerTwoLives">Player two's lives, as $045B holds them.</param>
    public HudModel(
        ReadOnlySpan<byte> playerOneScore,
        ReadOnlySpan<byte> playerTwoScore,
        ReadOnlySpan<byte> highScore,
        int playerOneLives,
        int playerTwoLives)
    {
        PlayerOneScore = Digits(playerOneScore, nameof(playerOneScore));
        PlayerTwoScore = Digits(playerTwoScore, nameof(playerTwoScore));
        HighScore = Digits(highScore, nameof(highScore));
        PlayerOneLives = playerOneLives;
        PlayerTwoLives = playerTwoLives;
    }

    /// <summary>Gets how many bytes of packed BCD one score is.</summary>
    public static int ScoreBytes => 3;

    /// <summary>Gets how many digits those bytes are drawn as.</summary>
    public static int ScoreDigits => ScoreBytes * 2;

    /// <summary>
    /// Gets how many cells a player's lives are drawn in. Seven: the routine at
    /// $046C counts a row down from six to zero, filling from the right.
    /// </summary>
    public static int LifeCells => 7;

    /// <summary>Gets player one's score, six characters, leading zeros blanked.</summary>
    public string PlayerOneScore { get; }

    /// <summary>Gets player two's score, six characters, leading zeros blanked.</summary>
    public string PlayerTwoScore { get; }

    /// <summary>Gets the high score, six characters, leading zeros blanked.</summary>
    public string HighScore { get; }

    /// <summary>
    /// Gets player one's lives, as the byte at $045A holds them. It is signed,
    /// and a negative count means the player is out - see
    /// <see cref="IsPlaying(int)"/>.
    /// </summary>
    public int PlayerOneLives { get; }

    /// <summary>Gets player two's lives, as the byte at $045B holds them.</summary>
    public int PlayerTwoLives { get; }

    /// <summary>
    /// Whether a player's lives are drawn at all. $046C reads the count and
    /// returns without writing anything when it is negative, which is how a
    /// player who has run out gets an empty row rather than a row of spaces.
    /// </summary>
    /// <param name="lives">The player's lives.</param>
    /// <returns>Whether the row is drawn.</returns>
    public static bool IsPlaying(int lives) => lives >= 0;

    /// <summary>
    /// How many life markers a count is drawn as. The row holds
    /// <see cref="LifeCells"/> of them and $046C stops when it runs out of
    /// cells, so a larger count draws no more than the row can hold.
    /// </summary>
    /// <param name="lives">The player's lives.</param>
    /// <returns>The markers to draw, none of them if the player is out.</returns>
    public static int LifeMarkers(int lives) => IsPlaying(lives) ? Math.Min(lives, LifeCells) : 0;

    // $3FB0. Each nibble is one digit, high first. A zero is blanked while nothing but zeros has
    // been seen and this is not the last digit; after that every digit is written, zero or not.
    private static string Digits(ReadOnlySpan<byte> score, string name)
    {
        if (score.Length != ScoreBytes)
        {
            throw new ArgumentException(
                $"A score is {ScoreBytes} bytes of BCD, not {score.Length}.",
                name);
        }

        Span<char> digits = stackalloc char[ScoreDigits];
        bool seen = false;

        for (int digit = 0; digit < ScoreDigits; digit++)
        {
            byte packed = score[digit / 2];
            int value = (digit % 2) == 0 ? packed >> 4 : packed & 0x0F;

            seen |= value != 0;
            digits[digit] = seen || digit == ScoreDigits - 1
                ? (char)('0' + value)
                : Blank;
        }

        return new(digits);
    }
}

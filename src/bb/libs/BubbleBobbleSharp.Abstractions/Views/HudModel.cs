// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The scores, the high score and the lives beside the level.</summary>
public sealed class HudModel
{
    private const char Blank = ' ';

    /// <summary>Initializes a new instance of the <see cref="HudModel"/> class.</summary>
    /// <param name="playerOneScore">Player one's score, as packed BCD.</param>
    /// <param name="playerTwoScore">Player two's score, as packed BCD.</param>
    /// <param name="highScore">The high score, as packed BCD.</param>
    /// <param name="playerOneLives">Player one's lives.</param>
    /// <param name="playerTwoLives">Player two's lives.</param>
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

    /// <summary>Gets how many cells a player's lives are drawn in.</summary>
    public static int LifeCells => 7;

    /// <summary>Gets player one's score, six characters, leading zeros blanked.</summary>
    public string PlayerOneScore { get; }

    /// <summary>Gets player two's score, six characters, leading zeros blanked.</summary>
    public string PlayerTwoScore { get; }

    /// <summary>Gets the high score, six characters, leading zeros blanked.</summary>
    public string HighScore { get; }

    /// <summary>Gets player one's lives.</summary>
    public int PlayerOneLives { get; }

    /// <summary>Gets player two's lives.</summary>
    public int PlayerTwoLives { get; }

    /// <summary>Whether a player's lives are drawn at all.</summary>
    /// <param name="lives">The player's lives.</param>
    /// <returns>Whether the row is drawn.</returns>
    public static bool IsPlaying(int lives) => lives >= 0;

    /// <summary>How many life markers a count is drawn as.</summary>
    /// <param name="lives">The player's lives.</param>
    /// <returns>The markers to draw, none of them if the player is out.</returns>
    public static int LifeMarkers(int lives) => IsPlaying(lives) ? Math.Min(lives, LifeCells) : 0;

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

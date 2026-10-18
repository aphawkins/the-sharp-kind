// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Settings for the Columnar transposition cipher.
/// </summary>
public sealed record ColumnarSettings : IColumnarSettings
{
    /// <summary>
    /// Gets the longest keyword allowed.
    /// </summary>
    public static int MaxKeywordLength => 25;

    /// <summary>
    /// Gets or sets the keyword of the cipher.
    /// </summary>
    /// <exception cref="ArgumentException">The keyword must be letters of the alphabet.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Length must be between 1 and 25 letters.</exception>
    public string Keyword
    {
        get => field ?? "A";

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length is 0 || value.Length > MaxKeywordLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Keyword),
                    $"Length must be between 1 and {MaxKeywordLength} letters.");
            }

            foreach (char letter in value)
            {
                if (Alphabet.IndexOf(letter) < 0)
                {
                    throw new ArgumentException("All keyword characters must be letters.", nameof(Keyword));
                }
            }

            field = value.ToUpperInvariant();
        }
    }
}

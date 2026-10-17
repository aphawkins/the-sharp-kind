// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Playfair cipher.
/// </summary>
/// <remarks>
/// Playfair works on pairs of letters, so it does not honour the pass-through contract of
/// <see cref="ICipher"/>. Non-letters are dropped, J is merged with I, an X separates a doubled
/// letter, and an X pads an odd final letter. Decrypting returns those extra letters.
/// </remarks>
/// <param name="settings">Settings.</param>
public sealed class Playfair(IPlayfairSettings settings) : ICipher
{
    private const int Size = 5;

    /// <inheritdoc />
    public string CipherName => "Playfair";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IPlayfairSettings Settings { get; } = settings;

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        return Crypt(Pair(Letters(plaintext)), 1);
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        string letters = Letters(ciphertext);

        return letters.Length % 2 == 0
            ? Crypt(letters, Size - 1)
            : throw new ArgumentException("Ciphertext must contain an even number of letters.", nameof(ciphertext));
    }

    /// <inheritdoc />
    public override string ToString() => CipherName;

    private static string Letters(string text)
    {
        StringBuilder letters = new(text.Length);

        foreach (char letter in text)
        {
            if (Alphabet.IndexOf(letter) >= 0)
            {
                letters.Append(char.ToUpperInvariant(letter) == 'J' ? 'I' : char.ToUpperInvariant(letter));
            }
        }

        return letters.ToString();
    }

    private static string Pair(string letters)
    {
        StringBuilder paired = new(letters.Length + 2);

        for (int i = 0; i < letters.Length; i++)
        {
            paired.Append(letters[i]);

            bool pairStart = paired.Length % 2 != 0;
            bool doubled = i + 1 < letters.Length && letters[i + 1] == letters[i];

            if (pairStart && doubled)
            {
                paired.Append('X');
            }
        }

        if (paired.Length % 2 != 0)
        {
            paired.Append('X');
        }

        return paired.ToString();
    }

    /// <summary>
    /// Substitutes each pair. A step of 1 encrypts and a step of 4 decrypts, being a step of -1 in a 5 wide grid.
    /// </summary>
    private string Crypt(string pairs, int step)
    {
        string grid = Settings.Grid;
        StringBuilder result = new(pairs.Length);

        for (int i = 0; i < pairs.Length; i += 2)
        {
            int a = grid.IndexOf(pairs[i], StringComparison.Ordinal);
            int b = grid.IndexOf(pairs[i + 1], StringComparison.Ordinal);
            (int rowA, int colA) = (a / Size, a % Size);
            (int rowB, int colB) = (b / Size, b % Size);

            if (rowA == rowB)
            {
                result
                    .Append(grid[(rowA * Size) + ((colA + step) % Size)])
                    .Append(grid[(rowB * Size) + ((colB + step) % Size)]);
            }
            else if (colA == colB)
            {
                int nextRowA = (rowA + step) % Size;
                int nextRowB = (rowB + step) % Size;
                result
                    .Append(grid[(nextRowA * Size) + colA])
                    .Append(grid[(nextRowB * Size) + colB]);
            }
            else
            {
                result
                    .Append(grid[(rowA * Size) + colB])
                    .Append(grid[(rowB * Size) + colA]);
            }
        }

        return result.ToString();
    }
}

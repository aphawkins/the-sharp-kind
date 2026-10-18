// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Rearranges the letters of a text, leaving every other character where it was.
/// </summary>
/// <remarks>
/// Keeping non-letters in place lets the transposition ciphers honour the pass-through contract of
/// <see cref="ICipher"/>: only the letters, upper cased, are moved between the letter positions.
/// </remarks>
internal static class Transposition
{
    /// <summary>
    /// Moves the letters of <paramref name="text"/> so that the k-th letter out is letter <c>order[k]</c> in.
    /// </summary>
    /// <param name="text">The text to transpose.</param>
    /// <param name="order">Builds the reading order for a given number of letters.</param>
    /// <param name="isEncrypting">Apply the order when encrypting, undo it when decrypting.</param>
    /// <returns>The transposed text.</returns>
    internal static string Apply(string text, Func<int, int[]> order, bool isEncrypting)
    {
        char[] chars = text.ToCharArray();
        List<int> positions = [];

        for (int i = 0; i < chars.Length; i++)
        {
            if (Alphabet.IndexOf(chars[i]) >= 0)
            {
                positions.Add(i);
            }
        }

        int[] permutation = order(positions.Count);
        char[] letters = new char[positions.Count];

        for (int k = 0; k < letters.Length; k++)
        {
            if (isEncrypting)
            {
                letters[k] = text[positions[permutation[k]]];
            }
            else
            {
                letters[permutation[k]] = text[positions[k]];
            }
        }

        for (int k = 0; k < letters.Length; k++)
        {
            chars[positions[k]] = char.ToUpperInvariant(letters[k]);
        }

        return new string(chars);
    }
}

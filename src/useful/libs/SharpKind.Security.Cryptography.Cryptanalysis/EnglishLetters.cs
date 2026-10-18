// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography.Cryptanalysis;

/// <summary>
/// The 26 letters of the English alphabet, as the cryptanalysis scores them.
/// </summary>
internal static class EnglishLetters
{
    /// <summary>
    /// The number of letters.
    /// </summary>
    internal const int Count = 26;

    /// <summary>
    /// Gets the position of a letter in the alphabet, ignoring case.
    /// </summary>
    /// <param name="letter">The letter to look up.</param>
    /// <returns>The zero based position, or -1 if <paramref name="letter"/> is not a letter of the alphabet.</returns>
    internal static int IndexOf(char letter)
        => letter switch
        {
            >= 'A' and <= 'Z' => letter - 'A',
            >= 'a' and <= 'z' => letter - 'a',
            _ => -1,
        };
}

// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Playfair key generator.
/// </summary>
internal static class PlayfairSettingsGenerator
{
    /// <summary>
    /// Makes a keyword of the grid letters in a random order, which gives a random grid.
    /// </summary>
    /// <returns>The new settings.</returns>
    public static PlayfairSettings Generate()
    {
        char[] keyword = [.. PlayfairSettings.GridLetters];
        RandomNumberGenerator.Shuffle(keyword);

        return new PlayfairSettings() { Keyword = new string(keyword) };
    }
}

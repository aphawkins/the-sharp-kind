// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// ADFGVX key generator.
/// </summary>
internal static class AdfgvxSettingsGenerator
{
    /// <summary>
    /// Shuffles the square and picks a random keyword of the same length. The labels stay the same.
    /// </summary>
    /// <param name="settings">The settings to start from.</param>
    /// <returns>The new settings.</returns>
    public static AdfgvxSettings Generate(IAdfgvxSettings settings)
    {
        char[] square = [.. settings.Square];
        RandomNumberGenerator.Shuffle(square);

        char[] keyword = new char[settings.Keyword.Length];

        for (int i = 0; i < keyword.Length; i++)
        {
            keyword[i] = (char)('A' + RandomNumberGenerator.GetInt32(Alphabet.Length));
        }

        return new AdfgvxSettings(settings.Labels, new string(square), new string(keyword));
    }
}

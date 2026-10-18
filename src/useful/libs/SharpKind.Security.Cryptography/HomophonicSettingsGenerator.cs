// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Homophonic key generator.
/// </summary>
internal static class HomophonicSettingsGenerator
{
    /// <summary>
    /// Shuffles the homophones between the plaintext characters. Each character keeps its number of homophones.
    /// </summary>
    /// <param name="settings">The settings to start from.</param>
    /// <returns>The new settings.</returns>
    public static HomophonicSettings Generate(IHomophonicSettings settings)
    {
        string[] symbols = [.. settings.Homophones.Values.SelectMany(static symbols => symbols)];
        RandomNumberGenerator.Shuffle(symbols);

        Dictionary<char, IReadOnlyList<string>> homophones = [];
        int next = 0;

        foreach ((char plain, IReadOnlyList<string> old) in settings.Homophones)
        {
            homophones[plain] = symbols[next..(next + old.Count)];
            next += old.Count;
        }

        return new HomophonicSettings(homophones);
    }
}

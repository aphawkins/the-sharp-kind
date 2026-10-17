// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The homophonic substitution cipher: each plaintext character has several possible cipher symbols.
/// </summary>
/// <remarks>
/// Each use of a character takes the next of its homophones in turn, so encrypting is repeatable.
/// Every symbol is the same length, so decrypting needs no separators. Characters that have no
/// homophones pass through, except letters A-Z, which must have homophones.
/// </remarks>
/// <param name="settings">Settings.</param>
public sealed class Homophonic(IHomophonicSettings settings) : ICipher
{
    /// <inheritdoc />
    public string CipherName => "Homophonic";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IHomophonicSettings Settings { get; } = settings;

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        Dictionary<char, int> uses = [];
        StringBuilder result = new(plaintext.Length * Settings.SymbolLength);

        foreach (char character in plaintext)
        {
            char upper = char.ToUpperInvariant(character);

            if (!Settings.Homophones.TryGetValue(upper, out IReadOnlyList<string>? symbols))
            {
                result.Append(Alphabet.IndexOf(character) < 0
                    ? character
                    : throw new ArgumentException($"Letter '{character}' has no homophones.", nameof(plaintext)));
                continue;
            }

            int use = uses.GetValueOrDefault(upper);
            uses[upper] = use + 1;
            result.Append(symbols[use % symbols.Count]);
        }

        return result.ToString();
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        Dictionary<string, char> characters = [];
        foreach ((char plain, IReadOnlyList<string> symbols) in Settings.Homophones)
        {
            foreach (string symbol in symbols)
            {
                characters[symbol] = plain;
            }
        }

        int length = Settings.SymbolLength;
        StringBuilder result = new(ciphertext.Length);
        int i = 0;

        while (i < ciphertext.Length)
        {
            if (i + length <= ciphertext.Length
                && characters.TryGetValue(ciphertext.Substring(i, length).ToUpperInvariant(), out char decrypted))
            {
                result.Append(decrypted);
                i += length;
            }
            else if (Alphabet.IndexOf(ciphertext[i]) < 0)
            {
                result.Append(ciphertext[i++]);
            }
            else
            {
                throw new ArgumentException($"Unknown symbol at position {i}.", nameof(ciphertext));
            }
        }

        return result.ToString();
    }

    /// <inheritdoc />
    public override string ToString() => CipherName;
}
